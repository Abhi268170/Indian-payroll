---
title: "feat: Auth + Tenant Provisioning (Chunk 1)"
type: feat
status: active
date: 2026-05-14
origin: docs/brainstorms/chunk1-auth-tenant-requirements.md
deepened: 2026-05-14
---

# feat: Auth + Tenant Provisioning (Chunk 1)

## Overview

Establishes the foundational dependency chain for the entire system:
tenant exists → schema provisioned → users belong to tenant → JWT carries tenant_id + roles → every request is schema-bound and claim-verified.

Nothing else can be tested end-to-end until this lands.

---

## Problem Frame

The scaffold has DBContexts, entities, interfaces, and middleware stubs — but no migrations, no controllers, no MediatR handlers, no seeds, and no integration tests. This plan produces all of those for auth + tenancy only. Employee, payroll, and salary features are out of scope.

See origin: `docs/brainstorms/chunk1-auth-tenant-requirements.md`

---

## Requirements Trace

- R1. PlatformDbContext migration covers Identity, Tenants, OpenIddict stores in public schema
- R2. `POST /api/tenants` requires SuperAdmin policy; 409 on slug conflict
- R3. Slug validation: lowercase alphanumeric + hyphens, 3–63 chars, no leading/trailing hyphens
- R4. Provisioning synchronous: response only after schema creation + migration succeed
- R5. `PATCH /api/tenants/{id}/suspend` — invalidates all refresh tokens for tenant users
- R6. `POST /connect/token` supports password grant; internal client seeded at startup
- R7. Access token: self-contained RS256 JWT, 15-min lifetime, claims: sub, email, tenant_id, tenant_schema, roles
- R8. Refresh token: reference token (stored in OpenIddict tables), 7-day lifetime, revocable
- R9. `/connect/token` rate-limited: 5 req/IP/min
- R10. TenantResolutionMiddleware: subdomain → Redis cache → DB; 404 unknown; 403 suspended
- R11. Non-tenant domain (no subdomain) passes through — used by SuperAdmin and token endpoint
- R12. TenantClaimValidationMiddleware: post-auth, JWT tenant_id must match ITenantContext.TenantId; mismatch → 403
- R13. Six roles seeded: SuperAdmin, OrgAdmin, HRManager, PayrollManager, FinanceViewer, Employee
- R14. SuperAdmin user seeded from env vars at startup (idempotent)
- R15. SuperAdmin: IsSuperAdmin = true, TenantId = null
- R16–R20. Integration tests: tenant creation, token endpoint, cross-tenant 403, role 403, real Postgres

**Origin actors:** A1 (Platform Super Admin), A2 (Tenant User), A3 (OpenIddict Server)

**Origin flows:** F1 (Tenant Provisioning), F2 (User Authentication), F3 (Tenant-Scoped Request)

**Origin acceptance examples:**
- AE1 (covers R2, R3, R4) — POST /api/tenants → 201 + schema with all tables
- AE2 (covers R7, R8) — access token is JWT; refresh token is opaque
- AE3 (covers R12) — acme JWT rejected on beta subdomain → 403
- AE4 (covers R5, R8) — suspended tenant → refresh token revoked; access expires ≤15 min

---

## Scope Boundaries

- No employees, salary structures, payroll runs, or tenant-data-level features
- No authorization code flow or PKCE — password flow only (API testing)
- No email verification or password reset
- No tenant self-registration UI — SuperAdmin API only
- No invite-based user creation — deferred to Chunk 2
- No Hangfire per-tenant isolation — deferred to Chunk 2
- No React frontend — API + tests only

---

## Context & Research

### Relevant Code and Patterns

- `src/Payroll.Infrastructure/Persistence/PlatformDbContext.cs` — extends IdentityDbContext, OpenIddict stores, public schema
- `src/Payroll.Infrastructure/Persistence/PayrollDbContext.cs` — tenant-schema-bound via ITenantContext
- `src/Payroll.Infrastructure/Persistence/TenantContext.cs` — scoped, throws on unresolved access
- `src/Payroll.Infrastructure/Persistence/TenantModelCacheKeyFactory.cs` — per-schema EF model cache key
- `src/Payroll.Infrastructure/Middleware/TenantResolutionMiddleware.cs` — subdomain extraction, Redis fallback
- `src/Payroll.Infrastructure/Persistence/ApplicationUser.cs` — TenantId?, EmployeeId?, IsSuperAdmin
- `src/Payroll.Domain/Entities/Tenant.cs` — private ctor, static Create(), Suspend()/Activate()
- `src/Payroll.Domain/Interfaces/ITenantRepository.cs` — GetBySlugAsync, GetByIdAsync, AddAsync
- `src/Payroll.Api/Program.cs` — OpenIddict wired, `UseReferenceAccessTokens()` must be removed
- `.claude/skills/security/security.md` — canonical auth/tenant patterns for this project
- `.claude/skills/testing/testing.md` — Testcontainers fixture pattern, TestJwtFactory, WAF pattern

### Institutional Learnings

- No `docs/solutions/` entries yet — first captured learnings should come from this chunk
- Three enforcement layers required: TenantResolutionMiddleware (pre-auth) + TenantClaimValidationMiddleware (post-auth) + TenantValidationBehaviour (MediatR pipeline)
- `TenantModelCacheKeyFactory` already handles per-schema EF model caching — do not alter it
- `PayrollDbContext` uses `AddDbContextFactory` (not `AddDbContext`) — always resolve via `IDbContextFactory<PayrollDbContext>`

### External References

- Security skill (`security.md`): token config, custom claims, tenant double-lock, rate limiting, Data Protection
- Testing skill (`testing.md`): PostgresFixture, PayrollApiFactory, TestJwtFactory, TenantIsolationTests

---

## Key Technical Decisions

- **Remove `UseReferenceAccessTokens()`; keep `UseReferenceRefreshTokens()`**: Access tokens become self-contained RS256 JWTs (no DB hit per request); refresh tokens remain revocable reference tokens (7-day, DB-backed). Rationale: scale without DB per-request; instant revocation via refresh token for compliance events.
- **RS256 asymmetric signing**: `AddDevelopmentSigningCertificate()` in dev (already in Program.cs); production uses file-based cert from Docker secret. Matches security skill directive — not symmetric.
- **Data Protection keys persisted to PlatformDbContext**: Survives container restarts; `PersistKeysToDbContext<PlatformDbContext>()`. Requires `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` NuGet package (not yet in Infrastructure.csproj).
- **Schema provisioning uses compensating action, not distributed transaction**: EF Core transactions cannot span two `DbContext` instances (each holds its own `NpgsqlConnection`). `CreateTenantHandler` uses a staged compensating action: (1) INSERT tenant row + `SaveChangesAsync()`; (2) `CREATE SCHEMA IF NOT EXISTS`; (3) `MigrateAsync()`; on failure at step 2 or 3 → `DROP SCHEMA {schema} CASCADE` + delete tenant row + `SaveChangesAsync()`. This is logged and explicitly tested. A shared-connection approach (`UseTransaction` across contexts) was considered but complicates the `IDbContextFactory<PayrollDbContext>` pattern already in use.
- **Three-layer tenant enforcement**: (1) TenantResolutionMiddleware pre-auth sets schema; (2) TenantClaimValidationMiddleware post-auth cross-checks JWT claim; (3) TenantValidationBehaviour in MediatR pipeline as final guard.
- **TenantClaimValidationMiddleware uses four-case predicate**: Not just `IsResolved == false`. All four cases must be handled: (IsResolved=false, no claim) → pass (SuperAdmin on base domain); (IsResolved=true, claim matches) → pass; (IsResolved=true, claim mismatches) → 403; (IsResolved=false, claim present) → 403 (tenant user on base domain). The last case was previously unguarded.
- **Middleware ordering is load-bearing**: `TenantResolutionMiddleware` → `UseAuthentication()` → `TenantClaimValidationMiddleware` → `UseAuthorization()`. Must not be reordered.
- **Migrations run in Program.cs before app.Run()**: `PlatformDbContext.Database.MigrateAsync()` called inline at startup, before `SeedDataService` runs. This guarantees table existence before Data Protection bootstraps and before seeds execute. Avoids the chicken-and-egg problem between `DataProtectionKeys` table creation and first Data Protection key write.
- **TestJwtFactory isolation invariant**: Test signing key is generated in-memory inside the test assembly only. It must never appear in any config file. WAF `ConfigureTestServices` adds it to OpenIddict validation options for the test host. A negative test verifies that a token signed with the test key is rejected by a production-mode WAF instantiation.
- **Token lifetime**: Access token 15 min, refresh token 7 days (per security skill).
- **SuperAdmin flow**: No subdomain → middleware passes through without setting ITenantContext. `IsSuperAdmin = true` users have `TenantId = null` — token carries no `tenant_id` claim.
- **Rate limiting**: 5 req/IP/min on `/connect/token` (per security skill — not 10).
- **Account lockout DoS tradeoff accepted**: IP-scoped rate limiting (5 req/IP/min) does not prevent a targeted multi-IP lockout of a known email. `DefaultLockoutTimeSpan = 15 minutes` creates a 15-minute DoS window per targeted account. This tradeoff is explicitly accepted for Chunk 1 — user-scoped + IP-keyed progressive delay or CAPTCHA is deferred to a later security hardening pass. The risk is documented, not silent.
- **`IAllowWithoutTenant` enforcement via NetArchTest**: Platform-level commands (SuperAdmin only, no tenant context) implement `IAllowWithoutTenant`. A NetArchTest rule gates this: types implementing `IAllowWithoutTenant` must reside in the `Payroll.Application.Commands.Platform` namespace. Convention: every such command's controller endpoint must carry `[Authorize(Policy = "SuperAdmin")]`.
- **`ITenantSeeder` extension point for Chunk 2**: `CreateTenantHandler` calls all registered `ITenantSeeder` implementations after migration. Chunk 1 ships with an empty collection. Chunk 2 registers its own seeder (default StatutoryToggles, etc.) without modifying the handler.
- **Tenant suspension evicts Redis cache**: `SuspendTenantHandler` must delete the Redis key `tenant:slug:{slug}` after committing. Without this, `TenantResolutionMiddleware` serves a stale `IsActive=true` cache hit for up to 10 minutes post-suspension, bypassing the revocation window (acceptance example AE4 would fail in production).

---

## Open Questions

### Resolved During Planning

- **Refresh token lifetime**: Requirements doc said 30 days; security skill says 7 days. Use **7 days** — security skill is authoritative for this project.
- **Rate limiting threshold**: Requirements doc said 10/min; security skill says 5/min. Use **5/min**.
- **Token signing**: Use development certs in dev (already in Program.cs via `AddDevelopmentSigningCertificate()`). Production cert loaded from Docker secret — out of scope for this chunk.
- **TenantClaimValidationMiddleware placement**: After `UseAuthentication()`, before `UseAuthorization()`. Audit log event on mismatch (string `"TENANT_MISMATCH"`).
- **DataProtection package**: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` must be added to `Payroll.Infrastructure.csproj`.

### Deferred to Implementation

- **Exact claim destination in OpenIddict principal**: Whether `tenant_id` is added to the `access_token` destination only or also `id_token` — depends on what OpenIddict 5.4.0 requires for custom claims to survive token serialization. Verify against OpenIddict docs at implementation time.
- **Compensating action retry semantics**: If `DROP SCHEMA CASCADE` itself fails during compensating rollback, the tenant row is in an inconsistent state. Acceptable for Chunk 1 (rare, admin-only). A retry-or-alert mechanism is deferred to a later resilience pass.
- **Bogus package in Payroll.Api.Tests**: Not present in `Payroll.Api.Tests.csproj`. Add if API-level tests need fixture data generators.
- **SeedDataService DB retry specifics**: Whether to use Polly or a simple exponential-backoff loop for the DB connectivity check on `StartAsync`. Either is acceptable — decide at implementation time based on what's already in the DI container.

---

## Output Structure

```
src/
  Payroll.Infrastructure/
    Persistence/
      Migrations/
        Platform/           ← PlatformDbContext migrations
        Tenant/             ← PayrollDbContext migrations
      TenantRepository.cs   ← ITenantRepository implementation
      EntityConfigurations/
        TenantConfiguration.cs
        ApplicationUserConfiguration.cs
    Middleware/
      TenantClaimValidationMiddleware.cs
    Services/
      SeedDataService.cs    ← IHostedService: roles, SuperAdmin, OpenIddict app
  Payroll.Application/
    Commands/
      Tenants/
        CreateTenantCommand.cs
        CreateTenantCommandValidator.cs
        CreateTenantHandler.cs
        SuspendTenantCommand.cs
        SuspendTenantHandler.cs
    Behaviours/
      TenantValidationBehaviour.cs
    Interfaces/
      IAllowWithoutTenant.cs
      ITenantSeeder.cs
  Payroll.Api/
    Controllers/
      TenantsController.cs
      AuthorizationController.cs
tests/
  Payroll.Api.Tests/
    Infrastructure/
      PostgresFixture.cs
      RedisFixture.cs
      PayrollWebApplicationFactory.cs
      TestJwtFactory.cs
      CollectionDefinitions.cs
    Auth/
      TenantProvisioningTests.cs
      TokenEndpointTests.cs
      TenantIsolationTests.cs
      RoleAuthorizationTests.cs
```

---

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

### Request lifecycle (tenant-scoped)

```
Client → [nginx: Host: acme.payroll.example.com]
  → TenantResolutionMiddleware
      slug = "acme"
      Redis HIT → TenantInfo{Id, Schema="tenant_acme", Slug, IsActive}
      ITenantContext.SetTenant(info)
  → UseAuthentication (OpenIddict validation)
      JWT verified (RS256 sig check, no DB hit)
      HttpContext.User populated with claims
  → TenantClaimValidationMiddleware
      jwt tenant_id == ITenantContext.TenantId  → pass
      mismatch                                  → 403 + audit log
  → UseAuthorization
      role policy check
  → Controller → IMediator.Send(command)
  → TenantValidationBehaviour
      ITenantContext.IsResolved == false → reject
  → Handler (uses ITenantRepository / IDbContextFactory<PayrollDbContext>)
      PayrollDbContext schema = "tenant_acme"
```

### Token issuance (password flow)

```
POST /connect/token  grant_type=password
  → OpenIddict server validates client_id + client_secret
  → AuthorizationController.Exchange()
      UserManager.FindByNameAsync(username)
      SignInManager.CheckPasswordSignInAsync(user, password)
      Build ClaimsPrincipal:
        sub         = user.Id
        email       = user.Email
        tenant_id   = user.TenantId  (absent for SuperAdmin)
        tenant_schema = tenant.Schema (absent for SuperAdmin)
        roles       = await UserManager.GetRolesAsync(user)
      return SignIn(principal, OpenIddictServerAspNetCoreDefaults)
  → Access token: self-contained JWT (15 min, RS256)
  → Refresh token: opaque reference (7 days, DB-stored)
```

### Tenant provisioning (compensating action pattern)

```
POST /api/tenants  [SuperAdmin JWT, base domain]
  → TenantsController → IMediator.Send(CreateTenantCommand)
  → CreateTenantHandler:
      1. Check slug uniqueness → 409 if exists
      2. Tenant.Create(displayName, slug)
      3. ITenantRepository.AddAsync(tenant) + SaveChangesAsync()  ← committed
      4. try:
           CREATE SCHEMA IF NOT EXISTS tenant_{slug}              ← from tenant.Schema
           PayrollDbContext.MigrateAsync() [scoped to new schema]
           ITenantSeeder[].SeedAsync() for each registered seeder
         catch:
           DROP SCHEMA tenant_{slug} CASCADE  (best-effort)
           DELETE tenant row + SaveChangesAsync()
           log both operations
           rethrow → 500
  → 201 {id, slug, schema}
```

---

## Implementation Units

- U1. **PlatformDbContext entity configurations + initial migration**

**Goal:** All public-schema tables created via EF migration. Data Protection keys persist across restarts.

**Requirements:** R1

**Dependencies:** None

**Files:**
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/TenantConfiguration.cs`
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/ApplicationUserConfiguration.cs`
- Modify: `src/Payroll.Infrastructure/Persistence/PlatformDbContext.cs` — add IDataProtectionKeyContext
- Modify: `src/Payroll.Infrastructure/Payroll.Infrastructure.csproj` — add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — wire Data Protection
- Modify: `src/Payroll.Api/Program.cs` — remove `UseReferenceAccessTokens()`, remove unused auth-code-flow endpoints, set token lifetimes, apply startup migration, protect Hangfire dashboard
- Create: `src/Payroll.Infrastructure/Persistence/Migrations/Platform/` — EF-generated migration

**Approach:**
- `TenantConfiguration`: unique index on `slug`, unique index on `schema`; `display_name` not null, max 200 chars; `created_at` timestamptz default UTC now
- `ApplicationUserConfiguration`: `tenant_id` nullable FK, `employee_id` nullable, `is_super_admin` bool default false
- `PlatformDbContext` implements `IDataProtectionKeyContext` by adding `DbSet<DataProtectionKey> DataProtectionKeys`
- Data Protection wired in `AddInfrastructure`: `services.AddDataProtection().PersistKeysToDbContext<PlatformDbContext>().SetApplicationName("IndianPayroll")`
- Token config in `Program.cs`: `options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))`, `options.SetRefreshTokenLifetime(TimeSpan.FromDays(7))`
- Remove `options.UseReferenceAccessTokens()` (access tokens become self-contained JWTs)
- Keep `options.UseReferenceRefreshTokens()` (refresh tokens remain DB-backed)
- **Remove unused attack surface**: remove `AllowAuthorizationCodeFlow()`, `SetAuthorizationEndpointUris("/connect/authorize")`, and `EnableAuthorizationEndpointPassthrough()` from the OpenIddict server configuration — these endpoints are out of scope and represent an open-redirect class vulnerability if left live
- **Protect Hangfire dashboard**: change `app.MapHangfireDashboard("/hangfire")` to `app.MapHangfireDashboard("/hangfire").RequireAuthorization("SuperAdmin")` — currently exposed unauthenticated, leaking job history including any PII in job arguments
- **Startup migration runner**: after `app.Build()` and before `app.Run()`, resolve `PlatformDbContext` via a scoped service and call `MigrateAsync()`. This guarantees the `DataProtectionKeys` table exists before Data Protection bootstraps and before `SeedDataService.StartAsync` fires.
- After wiring, run `dotnet ef migrations add InitialPlatform --context PlatformDbContext --project src/Payroll.Infrastructure --startup-project src/Payroll.Api`

**Patterns to follow:**
- `src/Payroll.Domain/Entities/Tenant.cs` — entity shape
- Security skill: `PersistKeysToDbContext<AuthDbContext>()` pattern

**Test scenarios:**
- Test expectation: none — DDL-only unit. Verify via: `dotnet ef migrations script --context PlatformDbContext` produces valid SQL with `tenants`, `asp_net_users`, `asp_net_roles`, `openiddict_*`, `data_protection_keys` tables.

**Verification:**
- Migration generates without error
- Migration SQL includes unique constraint on `tenants.slug`
- `UseReferenceAccessTokens()` removed from Program.cs
- `AllowAuthorizationCodeFlow()` and related endpoints removed from OpenIddict config
- `MapHangfireDashboard` requires `SuperAdmin` policy
- First startup against blank DB succeeds without Data Protection table errors

---

- U2. **PayrollDbContext initial migration**

**Goal:** Per-tenant schema tables defined as an EF migration, ready to be applied to any new tenant schema at provisioning time.

**Requirements:** R4 (provisioning applies this migration)

**Dependencies:** None (independent of U1)

**Files:**
- Create: `src/Payroll.Infrastructure/Persistence/EntityConfigurations/` — entity configs for all 10 PayrollDbContext entities (Employee, Department, Designation, Branch, CostCentre, SalaryComponent, EmployeeSalaryStructure, PayrollRun, AuditLog, StatutoryToggle)
- Create: `src/Payroll.Infrastructure/Persistence/Migrations/Tenant/` — EF-generated migration

**Approach:**
- EF `IEntityTypeConfiguration<T>` for each domain entity: primary keys, timestamps as `timestamptz`, soft-delete `IsDeleted` bool where needed, string length constraints, indexes
- `AuditLog`: no Update/Delete — enforce via PG trigger (add trigger SQL to migration's raw SQL block)
- `HasDefaultSchema` is set dynamically via `TenantContext.Schema` at DbContext construction — migrations use a placeholder schema (e.g., `public`) but apply to the resolved schema at runtime via `MigrateAsync()`
- Run: `dotnet ef migrations add InitialTenant --context PayrollDbContext --project src/Payroll.Infrastructure --startup-project src/Payroll.Api`

**Patterns to follow:**
- CLAUDE.md: reversible `Up` + `Down`, `timestamptz` for all timestamps, snake_case via `UseSnakeCaseNamingConvention()`
- Database skill: two-stage column drops, NOT NULL columns require defaults

**Test scenarios:**
- Test expectation: none — DDL-only unit. Verify by applying migration to a blank schema in a Testcontainers DB and asserting all tables exist.

**Verification:**
- Migration generates without error
- `dotnet build` passes (zero warnings)
- Applied to a blank schema: all 10 PayrollDbContext tables created

---

- U3. **Test infrastructure scaffold**

**Goal:** Integration test plumbing: Testcontainers fixture, WebApplicationFactory, TestJwtFactory. No feature tests yet — just the harness.

**Requirements:** R16–R20 (all integration tests depend on this)

**Dependencies:** U1, U2 (migrations must exist for WAF to boot)

**Files:**
- Create: `tests/Payroll.Api.Tests/Infrastructure/PostgresFixture.cs`
- Create: `tests/Payroll.Api.Tests/Infrastructure/RedisFixture.cs`
- Create: `tests/Payroll.Api.Tests/Infrastructure/PayrollWebApplicationFactory.cs`
- Create: `tests/Payroll.Api.Tests/Infrastructure/TestJwtFactory.cs`
- Create: `tests/Payroll.Api.Tests/Infrastructure/CollectionDefinitions.cs`
- Modify: `tests/Payroll.Api.Tests/Payroll.Api.Tests.csproj` — add `Bogus` if not present

**Approach:**
- `PostgresFixture`: `PostgreSqlBuilder().WithImage("postgres:16-alpine")`, runs PlatformDbContext + PayrollDbContext migrations on `InitializeAsync()` using a seeded Testcontainers DB
- `RedisFixture`: `RedisBuilder()`, provides connection string for test service replacement
- `PayrollWebApplicationFactory`: `WebApplicationFactory<Program>`, overrides DB connection strings and Redis connection string with Testcontainers instances via `ConfigureTestServices`; replaces email service with no-op
- `TestJwtFactory`: generates synthetic bearer tokens (self-signed RS256 key for tests). Must produce tokens with `tenant_id` claim that `TenantClaimValidationMiddleware` can parse. WAF must be configured to accept the test signing key.
- `CollectionDefinitions`: `[CollectionDefinition("Integration")]` using `ICollectionFixture<PostgresFixture>` + `ICollectionFixture<RedisFixture>`
- `Respawn` for between-test DB cleanup (already in test project)

**Patterns to follow:**
- Testing skill: `PostgresFixture`, `PayrollApiFactory`, `TestJwtFactory` patterns

**Test scenarios:**
- Happy path: WAF boots, GET /health returns 200
- Happy path: TestJwtFactory produces a decodable token with expected claims
- Edge case: fixture initialization is idempotent across test runs

**Verification:**
- `dotnet test --filter Category=Infrastructure` passes
- Factory boots against real Testcontainers Postgres without errors

---

- U4. **Startup seeds + Data Protection**

**Goal:** On startup, idempotently seed: 6 roles, 1 SuperAdmin user, 1 OpenIddict client application. Data Protection keys store in DB.

**Requirements:** R6 (token endpoint needs registered client), R13, R14, R15

**Dependencies:** U1 (PlatformDbContext migration must exist), U3 (test infra for integration test)

**Files:**
- Create: `src/Payroll.Infrastructure/Services/SeedDataService.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — register `SeedDataService` as `IHostedService`

**Approach:**
- `SeedDataService : IHostedService` — runs after startup migration (which completes in Program.cs before app.Run(), guaranteeing all tables exist)
- **DB readiness**: `StartAsync` wraps DB operations in an exponential-backoff retry loop (3 attempts, 2/4/8s delays) to handle transient connectivity failures in Docker Compose startup sequencing
- **Multi-replica safety**: acquire a Redis distributed lock (`SeedLock:payroll-seed`) at the start of `StartAsync`, held for the duration of seeding; release on completion or exception. Prevents duplicate inserts when multiple API replicas start simultaneously
- Roles: loop over `Roles.SuperAdmin|OrgAdmin|HRManager|PayrollManager|FinanceViewer|Employee`; use `RoleManager<ApplicationRole>.RoleExistsAsync` before creating
- SuperAdmin: reads `SUPERADMIN_EMAIL` + `SUPERADMIN_PASSWORD` from `IConfiguration`; uses `UserManager<ApplicationUser>.FindByEmailAsync` before creating; sets `IsSuperAdmin = true`, `TenantId = null`; assigns `SuperAdmin` role
- OpenIddict app: use `IOpenIddictApplicationManager.FindByClientIdAsync("payroll-api")` before creating; create with password flow, `payroll.api` scope
- Expose a static `Roles` class in `Payroll.Domain` as constants — Domain layer is accessible to all layers, never magic strings

**Execution note:** Test-first — write an integration test that asserts roles + SuperAdmin + OpenIddict app exist after factory startup before writing `SeedDataService`.

**Patterns to follow:**
- Security skill: RBAC role constants, `IOpenIddictApplicationManager` seeding
- CLAUDE.md: idempotent seeds — check before create

**Test scenarios:**
- Happy path: after WAF startup, all 6 roles exist in `AspNetRoles`
- Happy path: SuperAdmin user exists with `IsSuperAdmin = true`
- Happy path: OpenIddict application registered with `client_id = "payroll-api"`
- Edge case: running seed twice does not create duplicates (idempotency)
- Error path: missing `SUPERADMIN_EMAIL` config → service throws `InvalidOperationException` with clear message

**Verification:**
- `dotnet test --filter Auth.SeedTests` green
- SuperAdmin can authenticate against token endpoint (tested in U6)

---

- U5. **ITenantRepository + tenant provisioning commands**

**Goal:** Implement ITenantRepository. Implement CreateTenant + SuspendTenant commands with full provisioning logic. Wire controller.

**Requirements:** R2, R3, R4, R5

**Dependencies:** U1 (PlatformDbContext migration), U2 (PayrollDbContext migration to apply), U3 (test infra), U4 (SuperAdmin for auth in tests)

**Files:**
- Create: `src/Payroll.Infrastructure/Persistence/TenantRepository.cs`
- Create: `src/Payroll.Application/Commands/Tenants/CreateTenantCommand.cs`
- Create: `src/Payroll.Application/Commands/Tenants/CreateTenantCommandValidator.cs`
- Create: `src/Payroll.Application/Commands/Tenants/CreateTenantHandler.cs`
- Create: `src/Payroll.Application/Commands/Tenants/SuspendTenantCommand.cs`
- Create: `src/Payroll.Application/Commands/Tenants/SuspendTenantHandler.cs`
- Create: `src/Payroll.Application/Interfaces/ITenantSeeder.cs`
- Create: `src/Payroll.Api/Controllers/TenantsController.cs`
- Modify: `src/Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — register `TenantRepository`, register `IEnumerable<ITenantSeeder>`
- Test: `tests/Payroll.Api.Tests/Auth/TenantProvisioningTests.cs`

**Approach:**
- `TenantRepository`: uses `PlatformDbContext`; `AddAsync` — EF tracking; `GetBySlugAsync` — `FirstOrDefaultAsync` with `AsNoTracking()`; `DeleteAsync` — needed for compensating rollback
- `CreateTenantHandler` — **compensating action pattern** (not a distributed transaction):
  1. Check slug uniqueness → 409 if exists
  2. `Tenant.Create(displayName, slug)`
  3. `ITenantRepository.AddAsync(tenant)` + `IUnitOfWork.SaveChangesAsync()` — tenant row committed
  4. Execute raw SQL `CREATE SCHEMA IF NOT EXISTS {schema}` via `PlatformDbContext.Database.ExecuteSqlRawAsync` — schema name from `tenant.Schema` property, never from user input
  5. Resolve `IDbContextFactory<PayrollDbContext>`, configure for new schema, call `MigrateAsync()`
  6. Call all registered `ITenantSeeder` implementations (empty collection in Chunk 1)
  7. On any exception at steps 4-6: log error, attempt `DROP SCHEMA {schema} CASCADE`, delete tenant row + `SaveChangesAsync()`, rethrow. Log compensation failure separately if it also fails.
  8. Return 201 on success
- `SuspendTenantHandler`:
  - Fetch tenant, call `Suspend()`, `SaveChangesAsync()`
  - Iterate users with `TenantId == tenant.Id`, call `IOpenIddictTokenManager.RevokeBySubjectAsync(userId)` for each
  - **Delete Redis cache key**: `IDistributedCache.RemoveAsync($"tenant:slug:{tenant.Slug}")` — ensures `TenantResolutionMiddleware` sees `IsActive=false` immediately, not after 10-min TTL (fixes AE4 correctness)
- `TenantsController`: thin, no logic. `[Authorize(Policy = "SuperAdmin")]` on class. `POST /api/tenants` → `IMediator.Send(new CreateTenantCommand(...))`. `PATCH /api/tenants/{id}/suspend` → `IMediator.Send(new SuspendTenantCommand(id))`
- `ITenantSeeder` interface defined in `Payroll.Application`: `Task SeedAsync(Guid tenantId, string schema, CancellationToken ct)`. Registered as `IEnumerable<ITenantSeeder>` in DI.
- Slug format validation in `CreateTenantCommandValidator`: regex `^[a-z0-9][a-z0-9\-]{1,61}[a-z0-9]$` (enforces R3)

**Execution note:** Test-first — write `TenantProvisioningTests.cs` (R16) before implementing handler.

**Patterns to follow:**
- `src/Payroll.Domain/Entities/Tenant.cs` — factory method, no raw constructors
- CLAUDE.md: MediatR pattern, thin controllers, repository interfaces
- Database skill: reversible migrations, `NOT NULL` constraints, `timestamptz`

**Test scenarios:**
- Happy path: `POST /api/tenants` as SuperAdmin → 201, `tenant_*` schema exists in DB with all PayrollDbContext tables. `Covers AE1.`
- Happy path: provisioned tenant appears in `public.tenants` with `IsActive = true`
- Edge case: duplicate slug → 409 Conflict
- Edge case: slug `"a"` (too short) → 400 validation error
- Edge case: slug `"-acme"` (leading hyphen) → 400 validation error
- Error path: migration fails mid-provisioning → tenant row absent from `public.tenants` after compensating rollback (verify via DB query); schema absent or dropped
- Happy path: `PATCH /api/tenants/{id}/suspend` as SuperAdmin → 204; `IsActive = false` in DB; Redis key `tenant:slug:{slug}` deleted. `Covers AE4.`
- Integration: suspended tenant → immediate subsequent request on that subdomain → 403 (cache evicted, middleware fetches fresh from DB). `Covers AE4.`
- Integration: suspended tenant → existing valid access token → continues to work until expiry (max 15 min); refresh token rejected immediately. `Covers AE4.`

**Verification:**
- `dotnet test --filter Auth.TenantProvisioningTests` green
- Schema `tenant_acme` created with all 10 PayrollDbContext tables

---

- U6. **OpenIddict token endpoint**

**Goal:** `POST /connect/token` password grant issues JWT access tokens + reference refresh tokens with correct custom claims. Rate limited.

**Requirements:** R6, R7, R8, R9

**Dependencies:** U1 (migration), U3 (test infra), U4 (OpenIddict app seeded, SuperAdmin user exists)

**Files:**
- Create: `src/Payroll.Api/Controllers/AuthorizationController.cs`
- Modify: `src/Payroll.Api/Program.cs` — add rate limiter policy and `app.UseRateLimiter()`
- Modify: `src/Payroll.Api/Program.cs` — map `/connect/token` to rate limiter
- Test: `tests/Payroll.Api.Tests/Auth/TokenEndpointTests.cs`

**Approach:**
- `AuthorizationController.Exchange()`: `[HttpPost("/connect/token")]`, `[AllowAnonymous]`, `[Consumes("application/x-www-form-urlencoded")]`
- Extract grant type from `OpenIddictRequest`; handle `IsPasswordGrantType()` only
- Look up user via `UserManager.FindByNameAsync(request.Username)` — username is email
- `SignInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)`
- Build `ClaimsIdentity` with `OpenIddictServerAspNetCoreDefaults.AuthenticationScheme`
- Claims and destinations:
  - `sub` → access_token + id_token
  - `email` → access_token + id_token
  - `tenant_id` → access_token only (absent for SuperAdmin)
  - `tenant_schema` → access_token only (absent for SuperAdmin)
  - `roles` → access_token only (via `OpenIddictConstants.Claims.Role`)
- For SuperAdmin (`user.IsSuperAdmin == true`): skip tenant claims
- For tenant users: look up tenant via `ITenantRepository.GetByIdAsync(user.TenantId.Value)` to get schema
- Return `SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)`
- Rate limiting: `AddRateLimiter` with fixed window: 5 permits, 1-minute window, no queue. Map `/connect/token` with `RequireRateLimiting("auth")`

**Execution note:** Test-first — write `TokenEndpointTests.cs` (R17) before implementing handler.

**Patterns to follow:**
- Security skill: custom claims pattern, token configuration
- Security skill: `AddRateLimiter` fixed window pattern for auth

**Test scenarios:**
- Happy path: valid OrgAdmin credentials → 200, `access_token` decodes to JWT with `tenant_id` + `roles` claims. `Covers AE2.`
- Happy path: valid SuperAdmin credentials → 200, `access_token` decodes to JWT with NO `tenant_id` claim
- Happy path: refresh token is opaque string (not decodable as JWT). `Covers AE2.`
- Edge case: unknown username → 400 `invalid_grant`
- Edge case: wrong password → 400 `invalid_grant`; 5th consecutive failure → account locked
- Edge case: locked account → 400 with lockout error
- Edge case: `grant_type=authorization_code` → 400 (only password flow in Chunk 1)
- Error path: 6th request within 1 minute from same IP → 429 Too Many Requests

**Verification:**
- `dotnet test --filter Auth.TokenEndpointTests` green
- Tenant user JWT decoded at jwt.io shows `tenant_id`, `tenant_schema`, `roles` in payload; SuperAdmin JWT shows `sub`, `email`, `roles` only (no `tenant_id` or `tenant_schema`)
- Refresh token is opaque (no `.` separators)

---

- U7. **TenantClaimValidationMiddleware + TenantValidationBehaviour**

**Goal:** Post-auth JWT tenant_id cross-validation. MediatR pipeline guard against unresolved tenant. All tenant isolation integration tests green.

**Requirements:** R12, and indirectly R10 (already implemented in TenantResolutionMiddleware stub)

**Dependencies:** U3 (test infra), U5 (provisioned tenant), U6 (tokens to validate)

**Files:**
- Create: `src/Payroll.Infrastructure/Middleware/TenantClaimValidationMiddleware.cs`
- Create: `src/Payroll.Application/Behaviours/TenantValidationBehaviour.cs`
- Create: `src/Payroll.Application/Interfaces/IAllowWithoutTenant.cs`
- Modify: `src/Payroll.Api/Program.cs` — register TenantClaimValidationMiddleware in correct position
- Modify: `tests/Payroll.Api.Tests/Architecture/ArchitectureTests.cs` — add IAllowWithoutTenant namespace rule
- Test: `tests/Payroll.Api.Tests/Auth/TenantIsolationTests.cs`
- Test: `tests/Payroll.Api.Tests/Auth/RoleAuthorizationTests.cs`

**Approach:**
- `TenantClaimValidationMiddleware.InvokeAsync` — **four-case predicate**:
  - If `context.User.Identity?.IsAuthenticated != true`: `await next(context)` (unauthenticated, handled by UseAuthorization)
  - Extract `tenantIdClaim = context.User.FindFirstValue("tenant_id")`
  - **Case: IsResolved=false + no claim** → `await next(context)` (SuperAdmin on base domain — legitimate)
  - **Case: IsResolved=false + claim present** → 403 + log `"TENANT_MISMATCH"` (tenant user calling base domain — unauthorized)
  - **Case: IsResolved=true + claim matches TenantContext.TenantId** → `await next(context)`
  - **Case: IsResolved=true + claim mismatches or absent** → 403 + log `"TENANT_MISMATCH"`
- `TenantValidationBehaviour<TRequest, TResponse>`: implements `IPipelineBehavior`; if `ITenantContext.IsResolved == false` AND request type is NOT `IAllowWithoutTenant` → throw `InvalidOperationException("Tenant context not resolved")`
- **`IAllowWithoutTenant`**: marker interface defined in `Payroll.Application.Interfaces`. `CreateTenantCommand` and `SuspendTenantCommand` implement it.
- **NetArchTest rule** added to `ArchitectureTests.cs`: types implementing `IAllowWithoutTenant` must have namespace starting with `Payroll.Application.Commands.Platform`
- **Middleware registration order in Program.cs**:
  ```
  app.UseMiddleware<TenantResolutionMiddleware>()
  app.UseAuthentication()
  app.UseMiddleware<TenantClaimValidationMiddleware>()
  app.UseAuthorization()
  ```
- Register `TenantValidationBehaviour` in `AddApplication()` pipeline

**Execution note:** Test-first — write isolation tests (R18, R19) before implementing middleware.

**Patterns to follow:**
- `src/Payroll.Infrastructure/Middleware/TenantResolutionMiddleware.cs` — middleware shape
- Security skill: `TenantClaimValidationMiddleware` pattern (exact pattern from skill)
- Testing skill: `TenantIsolationTests` cross-tenant query pattern

**Test scenarios:**
- Integration: OrgAdmin token for tenant A, request to tenant B subdomain → 403. `Covers AE3.`
- Integration: OrgAdmin token for tenant A, request to tenant A subdomain → 200
- Integration: suspended tenant subdomain → 403 (from TenantResolutionMiddleware, not claim validation)
- Integration: SuperAdmin token (no tenant_id claim) to base domain → 200
- Integration: SuperAdmin token to tenant subdomain (IsResolved=true, no claim) → 403
- Integration: OrgAdmin token sent to base domain without subdomain (IsResolved=false, claim present) → 403 on any non-MediatR endpoint (health endpoint excluded — not authenticated)
- Integration: negative test — token signed with TestJwtFactory key sent to production-mode WAF → 401 (not accepted)
- Role auth: Employee token on `POST /api/tenants` → 403. `Covers R19.`
- Role auth: OrgAdmin token on `POST /api/tenants` → 403 (OrgAdmin not SuperAdmin)
- Integration: TenantValidationBehaviour blocks handler when ITenantContext not resolved and request is tenant-scoped
- Architecture: NetArchTest rule — type implementing `IAllowWithoutTenant` outside `Payroll.Application.Commands.Platform` namespace fails the test

**Verification:**
- `dotnet test --filter "Auth."` all green
- Architecture tests still pass (`dotnet test --filter ArchitectureTests`)
- `dotnet build` zero warnings

---

## System-Wide Impact

- **Middleware order**: `Program.cs` middleware pipeline order is now load-bearing. Any future middleware addition must respect the documented order. Add a comment block in `Program.cs` explaining the ordering constraint.
- **TenantClaimValidationMiddleware four-case enforcement**: The middleware must check all four (IsResolved × claim-present) combinations — not just `IsResolved`. The previously unguarded case (IsResolved=false + claim present) allows tenant users to reach non-MediatR endpoints on the base domain without tenant challenge. This gap is closed by the four-case predicate in U7.
- **TenantValidationBehaviour + IAllowWithoutTenant convention**: Future platform-level commands must be placed in the `Payroll.Application.Commands.Platform` namespace and implement `IAllowWithoutTenant`. The NetArchTest rule enforces namespace placement. Convention requires a paired `[Authorize(Policy = "SuperAdmin")]` on the controller endpoint — this is doc-only, not compiler-enforced.
- **ITenantContext.IsResolved**: middleware and behaviour both branch on this flag. Ensure every code path that accesses `ITenantContext.TenantId` or `ITenantContext.Schema` first checks `IsResolved`.
- **ITenantSeeder collection for Chunk 2**: `CreateTenantHandler` iterates `IEnumerable<ITenantSeeder>` after migration. Chunk 2 adds its seeder without modifying the handler. Chunk 1 ships with an empty collection.
- **Hangfire job tenant context (future chunks)**: `ITenantContext.SetTenant` is HTTP-middleware-driven in Chunk 1. Hangfire jobs execute outside the HTTP pipeline. Future chunks with tenant-scoped Hangfire jobs must set `ITenantContext` from a Hangfire job filter (reads `TenantId` from job argument, calls `SetTenant` on a fresh scoped `ITenantContext`). The Chunk 1 interface must not be designed to assume HTTP-only invocation.
- **Data Protection keys in PlatformDbContext**: `PlatformDbContext` now also manages Data Protection key rows. Do not accidentally include these in tenant schema migrations.
- **OpenIddict stores in PlatformDbContext**: token revocation (`SuspendTenantHandler`) uses `IOpenIddictTokenManager` which queries OpenIddict's public-schema tables. If `PlatformDbContext` connection is unavailable, suspension will fail. No fallback — acceptable for admin operations.
- **Integration coverage**: cross-tenant data leakage can only be proved with real Postgres. `TenantIsolationTests` is the CI gate — it must run on every push. Do not allow it to be skipped or marked flaky.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| OpenIddict 5.x custom claim destination syntax differs from skill snippet | Verify at implementation time — check `OpenIddictConstants.Destinations` for access_token destination flag |
| Cross-context provisioning: EF transactions don't span two DbContext instances | Compensating action pattern in `CreateTenantHandler` (see Key Technical Decisions). Error path test in U5 verifies rollback |
| `TestJwtFactory` signing key accidentally accepted in production | Test key generated in-memory in test assembly only — never in config. Negative test: production-mode WAF rejects test-key token (U7 test scenario) |
| `AddDevelopmentSigningCertificate()` produces ephemeral cert per startup — tokens invalidated on restart in dev | Acceptable for Chunk 1 dev/test cycle; production cert via Docker secret is out of scope here |
| `SuspendTenantHandler` token revocation — `RevokeBySubjectAsync` may be slow for large tenants | Acceptable for Chunk 1 (admin-only, infrequent operation); async bulk revocation deferred |
| Rate limiter `app.UseRateLimiter()` must precede `app.MapControllers()` | Verify order in Program.cs at implementation time |
| Account lockout DoS: 5 wrong passwords from 5 different IPs locks known email for 15 min | Accepted tradeoff for Chunk 1 — documented, not silent. Progressive delay / IP-keyed lockout deferred to security hardening pass |
| SeedDataService multi-replica race: two instances seeding simultaneously | Redis distributed lock in `SeedDataService.StartAsync` prevents concurrent duplicate inserts (see U4) |
| Auth-code flow endpoint open-redirect if misconfigured | Removed explicitly in U1 — `AllowAuthorizationCodeFlow()` and related endpoints deleted from OpenIddict config |

---

## Sources & References

- **Origin document:** [docs/brainstorms/chunk1-auth-tenant-requirements.md](docs/brainstorms/chunk1-auth-tenant-requirements.md)
- Security patterns: `.claude/skills/security/security.md`
- Testing patterns: `.claude/skills/testing/testing.md`
- Existing scaffold: `src/Payroll.Api/Program.cs`, `src/Payroll.Infrastructure/`
- OpenIddict 5.x: [https://documentation.openiddict.com](https://documentation.openiddict.com)
