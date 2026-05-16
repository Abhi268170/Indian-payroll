---
title: "feat: Platform admin org detail page and lifecycle management"
type: feat
status: active
date: 2026-05-14
---

# feat: Platform admin org detail page and lifecycle management

## Overview

The platform admin (SuperAdmin) currently has a tenant list page with no ability to drill into an org. This plan adds a detail/show page for provisioned orgs, the ability to activate a suspended org (inverse of the existing suspend action), a resend-setup-email action for orgs whose admin never completed setup, and fixes a latent Redis cache eviction bug in `SuspendTenantHandler`.

---

## Problem Frame

SuperAdmin manages all orgs that have purchased the payroll product. The existing `/platform/orgs` list shows name, slug, status, and created date. There is no way to view full org metadata (schema name, admin email, full timestamps), no way to reactivate a suspended org, and no way to resend the admin setup email if it was missed. The `SuspendTenantHandler` also lacks Redis cache eviction, meaning a suspended org's JWT bearer tokens remain valid for up to 10 minutes after suspension — a correctness gap flagged in the auth provisioning plan.

---

## Requirements Trace

- R1. SuperAdmin can view full org details: display name, slug, schema, status, created date, admin email.
- R2. SuperAdmin can activate a suspended org (reverse of existing suspend).
- R3. SuperAdmin can resend the setup email to an org admin (e.g., if the link expired or was missed).
- R4. Suspending an org immediately evicts the Redis tenant cache so the token-block takes effect without waiting for TTL expiry.
- R5. The org list rows are clickable and navigate to the detail page.
- R6. All new backend endpoints require `[Authorize(Policy = "SuperAdmin")]` and target the public schema only.

---

## Scope Boundaries

- No bulk org management (batch suspend/activate).
- No edit of org display name, slug, or schema from the admin portal in this iteration.
- No employee count or org-level usage metrics on the detail page (cross-schema query, deferred).
- No impersonation / "log in as org admin" action.
- No audit log or event history for org lifecycle changes.
- `AdminEmail` is surface-read only — no ability to change the admin email or create additional admins from the portal.

### Deferred to Follow-Up Work

- Active employee count on the detail page requires a cross-schema Dapper query (deferred to a dedicated platform metrics unit).
- Edit org display name (deferred — needs separate validation and slug-change implications).

---

## Context & Research

### Relevant Code and Patterns

- `src/Payroll.Domain/Entities/Tenant.cs` — `Tenant.Suspend()` and `Tenant.Activate()` domain methods exist; `Activate()` has no corresponding command yet.
- `src/Payroll.Application/Commands/Platform/Tenants/SuspendTenantCommand.cs` + `SuspendTenantHandler.cs` — pattern to mirror for `ActivateTenantCommand`. Handler is currently missing Redis eviction.
- `src/Payroll.Application/Queries/Platform/ListTenantsQuery.cs` — query record + handler colocated, implements `IAllowWithoutTenant`, uses `ITenantRepository`.
- `src/Payroll.Application/DTOs/TenantDto.cs` — current DTO: `Id, DisplayName, Slug, IsActive, CreatedAt`. Needs `Schema` and `AdminEmail`.
- `src/Payroll.Api/Controllers/TenantsController.cs` — `[Authorize(Policy = "SuperAdmin")]` at class level; new actions inherit automatically.
- `src/Payroll.Domain/Interfaces/IUserService.cs` — has `CreateOrgAdminAsync`, `GeneratePasswordResetTokenAsync`. Needs a new `GetOrgAdminEmailAsync(Guid tenantId)` method.
- `src/Payroll.Application/Commands/Platform/Tenants/CreateTenantHandler.cs` — pattern for generating reset token + dispatching welcome email job.
- `web/src/pages/platform/ProvisionOrgPage.tsx` — reference shape for platform admin React pages.
- `web/src/types/api.ts` — `TenantDto` interface (needs `schema?: string`, `adminEmail?: string`).
- `web/src/pages/platform/TenantsPage.tsx` — rows need `onClick` → navigate to detail.

### Institutional Learnings

- **Redis eviction on tenant state change is mandatory.** `SuspendTenantHandler` currently skips this. Any handler that calls `tenant.Suspend()` or `tenant.Activate()` must evict `tenant:slug:{slug}` from cache after committing. Without it, the middleware continues serving `IsActive=true` for up to 10 minutes.
- **`IAllowWithoutTenant` is required on all platform queries/commands.** `TenantValidationBehaviour` rejects requests where `ITenantContext.IsResolved == false` unless the marker interface is present.
- **Application layer cannot depend on `IDistributedCache` directly** (Infrastructure concern). Introduce `ITenantCacheService` in `Application/Interfaces/` with a single `EvictAsync(string tenantSlug, CancellationToken ct)` method. Infrastructure implements it via `IDistributedCache`.

### External References

- None needed — all patterns are well-established in the local codebase.

---

## Key Technical Decisions

- **`AdminEmail` is not denormalized to `Tenants` table.** Retrieving it requires a query against `AspNetUsers` via `IUserService.GetOrgAdminEmailAsync(Guid tenantId)`. This avoids a migration and stays consistent with Identity as the source of truth for user data.
- **`Schema` field is exposed in `TenantDto` (SuperAdmin only).** The endpoint is already `[Authorize(Policy = "SuperAdmin")]`. Exposing it helps with debugging and support.
- **`ITenantCacheService` abstraction in Application layer.** Keeps Application free of `IDistributedCache` (an Infrastructure type). Both `SuspendTenantHandler` and the new `ActivateTenantHandler` will inject this interface. Infrastructure wires it to `IDistributedCache.RemoveAsync`.
- **`ResendSetupEmailCommand` reuses existing token + email job infrastructure.** Calls `IUserService.GeneratePasswordResetTokenAsync` and `IEmailJobDispatcher.EnqueueWelcomeEmail` — the same operations `CreateTenantHandler` performs post-provisioning.
- **Detail page DTO is the same `TenantDto` (extended).** No separate `TenantDetailDto` needed — the list and detail can share the same record. `AdminEmail` and `Schema` default to `null` in list responses (the list handler won't populate them) and are populated only in the `GetTenantQuery` handler.
- **`GET /api/tenants/{id}` for the detail route.** The list endpoint returns full `TenantDto` objects. The detail endpoint returns one. Frontend caches them separately: `queryKey: ['platform-tenants']` for list, `queryKey: ['platform-tenant', id]` for detail.

---

## Open Questions

### Resolved During Planning

- **Should the list endpoint also return `AdminEmail`?** No — it requires N separate UserManager lookups or a non-trivial join. Return it only on the detail endpoint.
- **Should `ResendSetupEmail` check if the user has already set their password?** No — simpler and safer to always re-send. The link is a standard password reset token, expires in 24h, and re-use is harmless.
- **Should activating an already-active org or suspending an already-suspended org return an error?** The domain `Tenant.Suspend()` / `Tenant.Activate()` methods are idempotent (they just set `IsActive`). Return `204 No Content` in both cases — no error.

### Deferred to Implementation

- Whether `ITenantRepository` needs a new `GetByIdWithSchemaAsync` method or if `GetByIdAsync` already returns the `Schema` field (check at implementation time).
- Exact UserManager query shape for `GetOrgAdminEmailAsync` — likely `_userManager.Users.Where(u => u.TenantId == tenantId && ...).FirstOrDefaultAsync()`. Implementer should confirm the query via the existing `UserService` patterns.

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification.*

```
GET /api/tenants/{id}
  → GetTenantQuery(Id)
  → GetTenantHandler
      ITenantRepository.GetByIdAsync → Tenant
      IUserService.GetOrgAdminEmailAsync(tenant.Id) → string?
      → TenantDto (with Schema, AdminEmail populated)

POST /api/tenants/{id}/activate
  → ActivateTenantCommand(TenantId)
  → ActivateTenantHandler
      ITenantRepository.GetByIdAsync → Tenant
      tenant.Activate()
      IPlatformUnitOfWork.SaveChangesAsync()
      ITenantCacheService.EvictAsync(tenant.Slug)

POST /api/tenants/{id}/suspend  [EXISTING — fix only]
  → SuspendTenantHandler (add ITenantCacheService injection)
      ... existing logic ...
      ITenantCacheService.EvictAsync(tenant.Slug)  ← ADD

POST /api/tenants/{id}/resend-setup-email
  → ResendSetupEmailCommand(TenantId)
  → ResendSetupEmailHandler
      ITenantRepository.GetByIdAsync → Tenant (must be active)
      IUserService.GetOrgAdminEmailAsync(tenant.Id) → email
      IUserService.GeneratePasswordResetTokenAsync(email) → token
      build setPasswordUrl (BaseUrl + /set-password?token=&email=&slug=)
      IEmailJobDispatcher.EnqueueWelcomeEmail(email, slug, setPasswordUrl)
```

---

## Implementation Units

- U1. **Extend TenantDto + ITenantCacheService + GetTenantQuery + GET /api/tenants/{id}**

**Goal:** Surface full org metadata (including schema and admin email) via a new `GET /api/tenants/{id}` endpoint. Also introduces the `ITenantCacheService` abstraction used by U2.

**Requirements:** R1, R6

**Dependencies:** None

**Files:**
- Modify: `src/Payroll.Application/DTOs/TenantDto.cs`
- Create: `src/Payroll.Application/Interfaces/ITenantCacheService.cs`
- Modify: `src/Payroll.Domain/Interfaces/IUserService.cs`
- Modify: `src/Payroll.Infrastructure/Services/UserService.cs`
- Create: `src/Payroll.Infrastructure/Services/TenantCacheService.cs`
- Create: `src/Payroll.Application/Queries/Platform/GetTenantQuery.cs`
- Modify: `src/Payroll.Api/Controllers/TenantsController.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/Payroll.Api.Tests/Platform/TenantDetailTests.cs`

**Approach:**
- Add `Schema` and `AdminEmail` to `TenantDto` as **non-positional init properties** (not positional constructor parameters): `public string? Schema { get; init; }` and `public string? AdminEmail { get; init; }`. This keeps all existing `new TenantDto(...)` construction sites — particularly `ListTenantsHandler` — unchanged. `GetTenantHandler` populates them via object initializer syntax. Adding them as positional parameters would be a compile-breaking change under `TreatWarningsAsErrors=true`.
- `ITenantCacheService` has a single method: `Task EvictAsync(string tenantSlug, CancellationToken ct = default)`. Infrastructure implementation calls `IDistributedCache.RemoveAsync($"tenant:slug:{slug}")`.
- Add `Task<string?> GetOrgAdminEmailAsync(Guid tenantId, CancellationToken ct = default)` to `IUserService`. Implementation queries `UserManager.Users` for users with `TenantId == tenantId` and `OrgAdmin` role; use `OrderBy(u => u.Id).FirstOrDefaultAsync()` for determinism when multiple OrgAdmin users exist. (`ApplicationUser` has no `CreatedAt` property — `Id` (sequential Guid) is the correct tie-breaker, no migration required.)
- **Note on role assignment timing:** verify in `CreateTenantHandler` that `OrgAdmin` role is assigned at user creation (not after password-set). If the role is assigned post-setup, this query returns null for orgs in the exact state `ResendSetupEmail` targets. If so, fall back to querying by `TenantId` alone for users with `EmailConfirmed == false`.
- `GetTenantQuery(Guid Id) : IRequest<TenantDto>, IAllowWithoutTenant` — colocate record and handler in the same file under `Queries/Platform/`.
- Handler injects `ITenantRepository` + `IUserService`. Throws `NotFoundException` if tenant not found. Controller maps to 404.
- Register `TenantCacheService` as `AddSingleton<ITenantCacheService, TenantCacheService>()` — matching `IDistributedCache`'s Singleton lifetime. `TenantCacheService` has no per-request state; Scoped would cause captive-dependency exceptions from Singleton contexts (Hangfire jobs, middleware); Transient creates unnecessary allocations per handler call.

**Patterns to follow:**
- `src/Payroll.Application/Queries/Platform/ListTenantsQuery.cs` — query + handler colocated, `IAllowWithoutTenant`.
- `src/Payroll.Infrastructure/Services/UserService.cs` — UserManager usage pattern.

**Test scenarios:**
- Happy path: provision a tenant, call `GET /api/tenants/{id}` as SuperAdmin → 200 with all fields populated, `adminEmail` matches provisioned email, `schema` is `tenant_{slug}`.
- 404 path: call `GET /api/tenants/{unknownGuid}` → 404.
- Auth gate (no token): call `GET /api/tenants/{id}` with no bearer token → 401.
- Auth gate (wrong role): call with a valid OrgAdmin token (not SuperAdmin) → 403.
- `adminEmail` null path: tenant exists but has no OrgAdmin user (edge case in data) → 200, `adminEmail` is null (no error).

**Verification:**
- `GET /api/tenants/{id}` returns 200 with `schema` and `adminEmail` populated.
- `GET /api/tenants/{unknownId}` returns 404.
- `GET` without auth returns 401 (no token); `GET` with wrong-role token returns 403.
- Tests pass. Build has zero warnings.

---

- U2. **ActivateTenantCommand + fix Redis eviction in SuspendTenantHandler**

**Goal:** Add activate endpoint (inverse of suspend). Fix `SuspendTenantHandler` to evict Redis cache immediately after committing, so suspension takes effect without waiting for cache TTL.

**Requirements:** R2, R4

**Dependencies:** U1 (introduces `ITenantCacheService`)

**Files:**
- Create: `src/Payroll.Application/Commands/Platform/Tenants/ActivateTenantCommand.cs`
- Create: `src/Payroll.Application/Commands/Platform/Tenants/ActivateTenantHandler.cs`
- Modify: `src/Payroll.Application/Commands/Platform/Tenants/SuspendTenantHandler.cs`
- Modify: `src/Payroll.Api/Controllers/TenantsController.cs`
- Modify: `tests/Payroll.Api.Tests/Auth/TenantProvisioningTests.cs` (constructor changes to SuspendTenantHandler break existing test setup)
- Test: `tests/Payroll.Api.Tests/Platform/TenantLifecycleTests.cs`

**Approach:**
- `ActivateTenantCommand(Guid TenantId) : IRequest, IAllowWithoutTenant` — mirrors `SuspendTenantCommand`.
- `ActivateTenantHandler` injects `ITenantRepository`, `IPlatformUnitOfWork`, `ITenantCacheService`. Calls `tenant.Activate()`, saves, then `cacheService.EvictAsync(tenant.Slug)`.
- `SuspendTenantHandler`: add `ITenantCacheService` injection, call `EvictAsync` after `SaveChangesAsync`.
- **Cache eviction on idempotent paths:** both handlers call `EvictAsync` unconditionally. This is intentional — evicting a warm entry for an already-active/suspended tenant is safe (next request re-reads from DB). Conditional eviction based on EF `ChangeTracker.HasChanges()` is an optional optimization deferred to follow-up.
- **`EvictAsync` failure contract (deferred to implementation):** decide whether a Redis eviction failure during suspend/activate should (a) propagate as a 500 (suspension not confirmed until cache is cleared) or (b) log a structured warning and proceed (best-effort, TTL still applies). This decision must be captured in `TenantCacheService` implementation — do not leave it to default exception propagation.
- Controller: `POST /api/tenants/{id}/activate` → 204 No Content (matches suspend pattern).

**Patterns to follow:**
- `src/Payroll.Application/Commands/Platform/Tenants/SuspendTenantCommand.cs` + `SuspendTenantHandler.cs`.

**Test scenarios:**
- Happy path activate: suspend a tenant, then activate → 204; subsequent API call with org-admin token returns 200 (not 403).
- Happy path suspend with cache eviction: suspend a tenant; immediately resolve tenant by slug (simulate what middleware does) → `IsActive=false` in the resolved result (cache was evicted, not stale).
- Idempotent suspend: call suspend twice on an already-suspended tenant → 204 both times (no error).
- Idempotent activate: call activate twice on an already-active tenant → 204 both times (no error).
- 404: activate unknown tenant ID → 404.
- Auth gate (no token): suspend/activate with no bearer token → 401.
- Auth gate (wrong role): suspend/activate with OrgAdmin token → 403.
- Integration: after suspend + eviction, a request bearing the suspended org's JWT to `GET /api/employees` returns 403 (middleware sees `IsActive=false` immediately, not after cache TTL). **This test class must use a real Testcontainers Redis instance shared between WebApplicationFactory and test assertions. Do not mock `ITenantCacheService` or `IDistributedCache` in this class — a mocked eviction cannot be observed by `RedisTenantResolver`, making the test pass vacuously.**

**Verification:**
- `POST /api/tenants/{id}/activate` returns 204.
- Suspending a tenant immediately reflects in middleware (Redis cache evicted).
- Tests pass.

---

- U3. **ResendSetupEmailCommand + POST /api/tenants/{id}/resend-setup-email**

**Goal:** Allow SuperAdmin to resend the org admin setup email (e.g., if the link expired or was missed).

**Requirements:** R3, R6

**Dependencies:** U1

**Files:**
- Create: `src/Payroll.Application/Commands/Platform/Tenants/ResendSetupEmailCommand.cs`
- Create: `src/Payroll.Application/Commands/Platform/Tenants/ResendSetupEmailHandler.cs`
- Modify: `src/Payroll.Api/Controllers/TenantsController.cs`
- Test: `tests/Payroll.Api.Tests/Platform/ResendSetupEmailTests.cs`

**Approach:**
- `ResendSetupEmailCommand(Guid TenantId) : IRequest, IAllowWithoutTenant`.
- Handler injects `ITenantRepository`, `IUserService`, `IEmailJobDispatcher`, `IOptions<EmailOptions>`.
- Throws `NotFoundException` if tenant not found. Controller maps to 404.
- Throws `DomainException("Tenant is suspended")` if `!tenant.IsActive` — no point resending setup email to a suspended org. Controller maps to 400.
- Calls `IUserService.GetOrgAdminEmailAsync(tenant.Id)` — if no admin email found, throws `NotFoundException("No org admin found for tenant")` → 404.
- Generates reset token via `IUserService.GeneratePasswordResetTokenAsync(email)`. The existing `GeneratePasswordResetTokenAsync` throws `DomainException("User not found")` (not null) if the user is missing — catch this and re-throw as `NotFoundException("Org admin account not found")` → 404, not 400.
- Builds `setPasswordUrl` using same logic as `CreateTenantHandler`. Enqueues via `IEmailJobDispatcher.EnqueueWelcomeEmail`.
- Controller: `POST /api/tenants/{id}/resend-setup-email` → 204 No Content.
- No rate-limiting in this iteration (SuperAdmin-only endpoint, low volume).

**Patterns to follow:**
- `src/Payroll.Application/Commands/Platform/Tenants/CreateTenantHandler.cs` — token generation + email dispatch pattern.

**Test scenarios:**
- Happy path: existing tenant with OrgAdmin user → 204; Hangfire job enqueued for `SendWelcomeEmailJob`.
- 404 tenant: unknown tenant ID → 404.
- 404 no admin: tenant exists but no OrgAdmin user → 404 (rare but guard against it).
- Auth gate (no token): no bearer token → 401.
- Auth gate (wrong role): valid OrgAdmin token (not SuperAdmin) → 403.

**Verification:**
- `POST /api/tenants/{id}/resend-setup-email` returns 204.
- Hangfire job enqueued (verifiable via Hangfire dashboard or job table query in tests).
- Tests pass.

---

- U4. **Frontend: types update + TenantsPage clickable rows**

**Goal:** Add new fields to frontend `TenantDto` type and make list rows navigate to the detail page.

**Requirements:** R1, R5

**Dependencies:** U1 (backend DTO extension)

**Files:**
- Modify: `web/src/types/api.ts`
- Modify: `web/src/pages/platform/TenantsPage.tsx`

**Approach:**
- Add `schema?: string` and `adminEmail?: string` to the `TenantDto` interface in `types/api.ts`.
- In `TenantsPage.tsx`, wrap each `<tr>` with an `onClick` handler: `() => navigate(\`/platform/orgs/${t.id}\`)`. Add `cursor-pointer` Tailwind class to the row.
- Import `useNavigate` from `react-router-dom` if not already present.

**Patterns to follow:**
- `web/src/pages/platform/TenantsPage.tsx` — existing table row structure.

**Test scenarios:**
- Test expectation: none — this unit contains no behavioral logic beyond navigation. The route change is validated by the detail page integration (U5).

**Verification:**
- Clicking a row in the org list navigates to `/platform/orgs/{id}`.
- TypeScript compiles with zero errors.

---

- U5. **OrgDetailPage.tsx + router entry**

**Goal:** Detail page for a single org. Shows all metadata, and surfaces Suspend / Activate / Resend setup email actions.

**Requirements:** R1, R2, R3, R5

**Dependencies:** U1, U2, U3, U4

**Files:**
- Create: `web/src/pages/platform/OrgDetailPage.tsx`
- Modify: `web/src/router.tsx`

**Approach:**
- Route: `/platform/orgs/:id` inside the existing `<RequireSuperAdmin><PlatformLayout /></RequireSuperAdmin>` parent. **Sibling ordering in router.tsx:** the static `orgs/new` route must appear before the dynamic `orgs/:id` route in the children array — React Router v6 ranks static before dynamic, but explicit ordering prevents misconfiguration where `:id` captures the literal string "new".
- Page fetches `GET /api/tenants/:id` via `useQuery({ queryKey: ['platform-tenant', id], queryFn: ... })`.
- Displays: Display Name, Slug, Schema, Status badge (Active / Suspended), Created date (formatted), Admin Email.
- Three action buttons (Suspend, Activate, Resend Setup Email), each wired to a `useMutation`:
  - Suspend: `POST /api/tenants/{id}/suspend` — only shown when `isActive === true`.
  - Activate: `POST /api/tenants/{id}/activate` — only shown when `isActive === false`.
  - Resend setup email: always visible.
- On mutation success: invalidate both `['platform-tenants']` (list) and `['platform-tenant', id]` (detail).
- Loading state: spinner or skeleton.
- Error state: display error message on fetch failure.
- Back link: `← All Orgs` navigates to `/platform/orgs`.
- No confirm dialog for suspend/activate in this iteration (keep it simple).

**Patterns to follow:**
- `web/src/pages/platform/ProvisionOrgPage.tsx` — `useMutation` + `queryClient.invalidateQueries` pattern.
- `web/src/pages/platform/TenantsPage.tsx` — Status badge, date formatting, `queryKey: ['platform-tenants']`.

**Test scenarios:**
- Happy path render: mock `GET /api/tenants/{id}` → renders all metadata fields correctly.
- Suspend button only shown when `isActive=true`; Activate button only shown when `isActive=false`.
- On suspend mutation success: `['platform-tenants']` and `['platform-tenant', id]` cache keys invalidated.
- Loading state shown before data resolves.
- Error state shown when fetch returns 404 or 500.

**Verification:**
- `/platform/orgs/:id` renders with full org metadata.
- Suspend/Activate/Resend actions update the UI after success.
- TypeScript compiles with zero errors.
- Manual walkthrough: provision org → click row → verify detail → suspend → detail shows Suspended badge → activate → badge reverts → resend setup email → Hangfire job in dashboard.

---

## System-Wide Impact

- **Interaction graph:** `TenantClaimValidationMiddleware` reads from the Redis cache to resolve tenant context. U2's Redis eviction fix means suspension takes effect immediately — this is the intended behavior, previously broken.
- **Error propagation:** `NotFoundException` thrown in handlers maps to `404 NotFound` via global exception middleware (existing behavior). All new handlers follow this pattern.
- **State lifecycle risks:** Resending the setup email generates a new password reset token. The old token (if any) remains valid until expiry — ASP.NET Identity does not invalidate prior tokens on generation. This is acceptable for setup emails (token is single-use and time-limited to 24h by default).
- **API surface parity:** No agent-facing tool changes in this iteration.
- **Integration coverage:** The suspend-then-request integration test (U2) is the most important cross-layer scenario — it exercises DB → cache eviction → middleware resolution → 403 in a single test. This cannot be proven by unit tests alone.
- **Unchanged invariants:** `POST /api/tenants` (provision), `POST /api/tenants/{id}/suspend` (existing) — contract unchanged. Suspend gains a side-effect (cache eviction) but its HTTP contract (`204 No Content`) is unchanged.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| `GetOrgAdminEmailAsync` is slow (UserManager query on every detail fetch) | Acceptable for a low-traffic SuperAdmin page. Add index on `AspNetUsers.TenantId` if not already present (verify at implementation time). |
| Resend email generates a new token but old token remains valid | Acceptable — setup emails expire in 24h. Out of scope to invalidate prior tokens. |
| `SuspendTenantHandler` change (adding cache eviction) is a behavioral change to an existing endpoint | Low risk — the eviction is additive. If Redis is unavailable, the eviction fails silently (verify `RemoveAsync` exception handling in `TenantCacheService` implementation). |
| `AdminEmail` null in `TenantDto` on list responses | List handler leaves it null. Frontend detail page will only call `GET /api/tenants/{id}` which populates it. Frontend list page does not render `adminEmail`, so null is safe. |

---

## Documentation / Operational Notes

- No new environment variables or configuration keys.
- No DB migration required (no schema changes).
- After shipping: MailHog at http://localhost:8025 verifies resend-setup-email in dev.
- Hangfire dashboard at http://localhost:5000/hangfire for job verification.

---

## Sources & References

- `src/Payroll.Application/Commands/Platform/Tenants/SuspendTenantCommand.cs`
- `src/Payroll.Application/Queries/Platform/ListTenantsQuery.cs`
- `src/Payroll.Application/DTOs/TenantDto.cs`
- `src/Payroll.Api/Controllers/TenantsController.cs`
- `web/src/pages/platform/TenantsPage.tsx`
- `web/src/pages/platform/ProvisionOrgPage.tsx`
- `docs/plans/2026-05-14-001-feat-auth-tenant-provisioning-plan.md` — Redis cache eviction decision, `IAllowWithoutTenant` pattern
