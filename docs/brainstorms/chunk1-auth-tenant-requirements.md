---
date: 2026-05-14
topic: chunk1-auth-tenant-provisioning
parent: docs/brainstorms/phase1-foundation-requirements.md
---

# Chunk 1 — Auth + Tenant Provisioning

## Problem Frame

Everything in this system depends on tenants existing and users being authenticated into
the right tenant. Until this chunk is complete, nothing can be tested end-to-end. This
chunk establishes the full dependency chain: tenant exists → schema provisioned → users
belong to tenant → token carries tenant_id + roles → every subsequent request is
schema-bound and claim-verified.

---

## Actors

- A1. Platform Super Admin — platform-level user; no tenant affiliation; provisions new tenants
- A2. Tenant User — any user belonging to a specific tenant (OrgAdmin, HRManager, etc.)
- A3. OpenIddict Server — issues tokens and validates them on each request

---

## Key Flows

- F1. Tenant Provisioning
  - **Trigger:** A1 calls `POST /api/tenants` with `displayName` and `slug`
  - **Actors:** A1, A3
  - **Steps:**
    1. Request authenticated as SuperAdmin; JWT tenant_id absent (platform-level call)
    2. Handler validates slug uniqueness and format (lowercase, hyphens only)
    3. Tenant row inserted into `public.tenants`
    4. `CREATE SCHEMA IF NOT EXISTS tenant_{slug}` executed
    5. PayrollDbContext migrations run against the new schema
    6. 201 Created returned with tenant id, slug, and schema name
  - **Outcome:** Tenant fully provisioned; schema ready for employee and payroll data
  - **Covered by:** R1, R2, R3, R4, R5

- F2. User Authentication
  - **Trigger:** Client posts `grant_type=password` with `username`, `password`, `scope` to `/connect/token`
  - **Actors:** A2, A3
  - **Steps:**
    1. OpenIddict validates credentials via ASP.NET Core Identity
    2. Handler builds principal with claims: `tenant_id`, `roles`, `sub`, `email`
    3. SuperAdmin call: `tenant_id` claim absent (not a tenant-scoped user)
    4. Token response: short-lived self-contained JWT access token + revocable reference refresh token
  - **Outcome:** Client holds a JWT access token valid for 15 minutes and a reference refresh token for silent renewal
  - **Covered by:** R6, R7, R8, R9

- F3. Tenant-Scoped Request
  - **Trigger:** Client calls any protected endpoint with Bearer token and tenant subdomain
  - **Actors:** A2, A3
  - **Steps:**
    1. TenantResolutionMiddleware extracts slug from subdomain, resolves TenantInfo, sets ITenantContext
    2. UseAuthentication validates JWT signature and populates HttpContext.User
    3. Post-auth tenant validation checks `User.FindFirst("tenant_id")` == resolved `TenantContext.TenantId`; mismatch → 403
    4. UseAuthorization checks role policies
    5. Request proceeds with schema-bound PayrollDbContext
  - **Outcome:** Request executes in correct tenant schema; cross-tenant access rejected at validation layer
  - **Covered by:** R10, R11, R12

---

## Requirements

**Platform migration**
- R1. `PlatformDbContext` migration covers: ASP.NET Core Identity tables, `tenants` table, OpenIddict stores — all in the `public` schema. Migration is reversible (Up + Down). Runs once at deploy time.

**Tenant provisioning**
- R2. `POST /api/tenants` requires `SuperAdmin` policy. Returns 409 if slug already exists.
- R3. Slug validation: lowercase alphanumeric + hyphens only, 3–63 characters, must not start or end with a hyphen.
- R4. Provisioning is synchronous: response is not returned until schema creation and PayrollDbContext migration both succeed. If either step fails, the tenant row is not committed (transactional rollback where possible; schema drop on failure).
- R5. Tenant can be suspended via `PATCH /api/tenants/{id}/suspend`. Suspension invalidates all refresh tokens for that tenant's users. Active JWT access tokens remain valid until expiry (max 15 min window).

**Token endpoint**
- R6. `POST /connect/token` supports `password` grant. Client authenticates with `client_id` + `client_secret` (seeded at startup for the internal API client).
- R7. Access token is a self-contained signed JWT, lifetime 15 minutes. Contains claims: `sub`, `email`, `tenant_id` (absent for SuperAdmin), `roles[]`.
- R8. Refresh token is a reference token (stored in OpenIddict tables), lifetime 30 days, sliding. Revocable.
- R9. `POST /connect/token` is rate-limited: max 10 requests per IP per minute.

**Tenant resolution + claim validation**
- R10. `TenantResolutionMiddleware` runs before `UseAuthentication`. Resolves tenant from subdomain slug via Redis cache (10-min TTL) falling back to `public.tenants`. Sets `ITenantContext`. Returns 404 if tenant not found; 403 if tenant suspended.
- R11. For requests from a non-tenant domain (no resolvable subdomain), middleware passes through without setting `ITenantContext`. Used by SuperAdmin and the token endpoint.
- R12. After `UseAuthentication`, a second enforcement layer validates that the authenticated user's `tenant_id` claim matches `ITenantContext.TenantId`. Mismatch → 403. Skipped when `ITenantContext.IsResolved` is false (platform-level calls).

**Roles**
- R13. Six roles seeded: `SuperAdmin`, `OrgAdmin`, `HRManager`, `PayrollManager`, `FinanceViewer`, `Employee`.
- R14. One SuperAdmin user seeded at startup from environment variables (`SUPERADMIN_EMAIL`, `SUPERADMIN_PASSWORD` via Docker secrets). Startup seed is idempotent — no duplicate created if user already exists.
- R15. SuperAdmin user has `IsSuperAdmin = true` and `TenantId = null`.

**Integration tests (TDD gate)**
- R16. Integration test: `POST /api/tenants` as SuperAdmin creates tenant row and schema; schema contains all PayrollDbContext tables.
- R17. Integration test: `POST /connect/token` with valid credentials returns JWT with correct `tenant_id` and `roles` claims.
- R18. Integration test: token issued for `tenant-a` rejected with 403 on `tenant-b` subdomain.
- R19. Integration test: role-based 403 — endpoint requiring `OrgAdmin` rejects authenticated `Employee` token.
- R20. All tests use Testcontainers (real PostgreSQL). No InMemory provider.

---

## Acceptance Examples

- AE1. **Covers R2, R3, R4.** Given SuperAdmin is authenticated, when `POST /api/tenants` with `{displayName: "Acme Corp", slug: "acme"}`, then response is 201 with `{id, slug: "acme", schema: "tenant_acme"}` and schema `tenant_acme` contains all PayrollDbContext tables.
- AE2. **Covers R7, R8.** Given valid password grant, when token issued, then `access_token` decodes at jwt.io to show `tenant_id` and `roles`, and `refresh_token` is an opaque string (not a JWT).
- AE3. **Covers R12.** Given user authenticated to `acme` tenant (JWT has `tenant_id = acme-guid`), when request made to `beta.yourpayroll.com`, then response is 403.
- AE4. **Covers R5, R8.** Given tenant `acme` is suspended, when user tries to refresh token, then 401 returned (refresh token revoked). Existing JWT access tokens expire within 15 minutes.

---

## Success Criteria

- `POST /api/tenants` provisions an isolated schema end-to-end; PayrollDbContext migrations run in it automatically.
- Token endpoint returns a JWT access token; claims verifiable without server round-trip (jwt.io decodes them).
- Refresh tokens are revocable; tenant suspension kills all refresh tokens for that tenant.
- Cross-tenant token use returns 403 — verified by integration test (R18).
- All R16–R20 integration tests green using real Postgres via Testcontainers.
- Architecture tests continue to pass after all new code is added.

---

## Scope Boundaries

- No employees, salary structures, payroll runs, or any tenant-data-level features.
- No authorization code flow or PKCE in this chunk (password flow only, for API testing).
- No email verification or password reset flows.
- No tenant self-registration UI — SuperAdmin API only.
- No invite-based user onboarding — user creation within a tenant is deferred to Chunk 2.
- No Hangfire per-tenant isolation (scaffolded in R2 of phase1 doc, deferred to later chunk).
- No frontend (React) in this chunk — API and tests only.

---

## Key Decisions

- **Hybrid token strategy**: Self-contained JWT access tokens (15 min) + revocable reference refresh tokens (30 days). Removes `UseReferenceAccessTokens()` from OpenIddict config; keeps `UseReferenceRefreshTokens()`. Rationale: no DB hit per request at scale; instant revocation via refresh token for compliance events (tenant suspension, user offboarding).
- **Synchronous provisioning**: Schema creation and migration run inline in the request. Simpler; provisioning is a rare admin operation. No Hangfire job needed here.
- **Post-auth tenant validation as a separate enforcement layer**: TenantResolutionMiddleware cannot validate JWT claims (runs before UseAuthentication). A second enforcement layer after UseAuthentication handles the cross-claim check. Keeps middleware ordering intact (schema must be set before handlers run).
- **Password flow only**: Simplest path for API integration testing. Auth code / PKCE added when the React frontend is wired up (later chunk).

---

## Dependencies / Assumptions

- Docker Compose infra is up (`db`, `redis`) for integration tests.
- `Testcontainers.PostgreSql` package available in `Payroll.Infrastructure.Tests` and `Payroll.Api.Tests`.
- OpenIddict 5.x is the version in use (consistent with .NET 8 and `OpenIddict.EntityFrameworkCore` reference in Infrastructure.csproj).
- Nginx subdomain routing exists in Docker Compose for local dev (`*.localhost` or custom `/etc/hosts` entries); integration tests inject the `Host` header directly without needing Nginx.

---

## Outstanding Questions

### Resolve Before Planning

*(none — all product decisions resolved)*

### Deferred to Planning

- [Affects R12][Technical] Best mechanism for post-auth tenant claim validation: second middleware vs. global authorization policy vs. custom `IAuthorizationHandler`. All are equivalent in behavior; choose based on what composes most cleanly with existing auth pipeline.
- [Affects R4][Technical] Schema creation uses raw SQL (`CREATE SCHEMA`) + programmatic EF migration (`dbContext.Database.MigrateAsync()`). Confirm transaction semantics: PostgreSQL DDL is transactional; verify rollback behavior if migration fails mid-run.
- [Affects R14][Technical] Startup seed mechanism: `IHostedService` on startup vs. migration data seed. Confirm idempotency approach.

---

## Next Steps

`/ce-plan` with this document as input.
