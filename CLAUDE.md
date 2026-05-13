# Indian Payroll SaaS — Claude Code Rules

## Behavioral Guidelines

Bias toward caution over speed. For trivial tasks, use judgment.

### 1. Think Before Coding

Before implementing:
- State assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them — don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

Minimum code that solves the problem. Nothing speculative.

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

Touch only what you must. Clean up only your own mess.

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it — don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

Every changed line must trace directly to the user's request.

### 4. Goal-Driven Execution

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria enable independent loops. Weak criteria ("make it work") require constant clarification.

---

## Project Overview

Multi-tenant Indian payroll SaaS. B2B, self-hosted, schema-per-tenant.
Stack: .NET 8 + React 18 + TypeScript + PostgreSQL 16 + Redis + Hangfire + MinIO + MailHog.
Reference product: Zoho Payroll. V1: new tax regime ONLY.

## Solution Structure

```
src/
  Payroll.Api/           # ASP.NET Core Web API — controllers, middleware, DI wiring
  Payroll.Application/   # MediatR commands/queries, FluentValidation, DTOs
  Payroll.Domain/        # Entities, value objects, domain events, enums — NO infrastructure deps
  Payroll.Infrastructure/# EF Core, Dapper, Redis, MinIO, Email, Hangfire registrations
  Payroll.Engine/        # Pure payroll calc library — NO I/O, NO DI, NO framework deps
web/
  src/                   # React 18 + Vite + TypeScript strict
```

---

## Non-Negotiable Domain Rules

- **Decimal only.** All monetary values, tax amounts, percentages: `decimal`. Never `float` or `double`. Zero exceptions.
- **New regime only in v1.** Never implement old regime logic. If a requirement touches old regime, mark `// DEFERRED: old-regime` and stop.
- **No hardcoded statutory values.** Tax slabs, PT rates, LWF amounts, PF limits — all come from DB config tables. Never magic numbers in engine or application code.
- **Payroll.Engine is pure.** No EF, no Redis, no HttpClient, no DI container, no `async`. Synchronous pure functions only. All inputs passed as parameters, all outputs returned as values.
- **Schema-per-tenant always.** Every EF query is schema-bound at DbContext construction. No query can cross tenant boundary at the DB level.

---

## Architecture Rules (Enforce via NetArchTest in CI)

```
Domain        → no dependencies on any other layer
Engine        → no dependencies on any layer (standalone)
Application   → depends on Domain only
Infrastructure→ depends on Application + Domain
Api           → depends on Application only (never Infrastructure directly, except DI root)
```

- No `new` for services in handlers — use constructor injection.
- No logic in controllers — thin controllers, all logic in MediatR handlers.
- No EF Core in Application layer — repositories/interfaces only.
- No Dapper in Application layer — use read-model interfaces.

---

## Code Quality Standards

- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in all projects. Zero warnings tolerated.
- Nullable reference types enabled (`<Nullable>enable</Nullable>`). Null-safety is mandatory.
- No `var` for non-obvious types — use explicit types when the RHS doesn't make the type clear.
- No `#pragma warning disable` without a comment explaining why.
- No `TODO` comments committed — create a GitHub issue instead.
- Methods max ~30 lines. If longer, extract.
- Cyclomatic complexity max 10 per method.
- No `catch (Exception e)` without logging and rethrowing or explicit justification.

**TypeScript (Frontend):**
- `strict: true` in tsconfig. No `any`. No `@ts-ignore` without explanation.
- No `as unknown as X` casts. Fix the type instead.
- Explicit return types on all exported functions.

---

## TDD Workflow

**Red → Green → Refactor. Always.**

1. Write failing test first.
2. Write minimum code to pass.
3. Refactor with tests green.

**Test placement:**
```
tests/
  Payroll.Engine.Tests/        # Unit tests — pure calc logic
  Payroll.Application.Tests/   # Unit tests — handler logic, validation
  Payroll.Infrastructure.Tests/# Integration tests — real Postgres via Testcontainers
  Payroll.Api.Tests/           # Integration tests — WebApplicationFactory + real DB
web/src/__tests__/             # Vitest component + hook tests
e2e/                           # Playwright E2E tests
```

**Coverage minimums (enforced in CI):**
- `Payroll.Engine`: 95%
- `Payroll.Application`: 80%
- `Payroll.Infrastructure`: integration tests (no line coverage gate)
- Frontend critical paths: 70%

**Engine tests:** Never mock calculators. Test real decimal arithmetic with exact expected values from statutory spec. Use `Bogus` for employee fixture data.

**Integration tests:** Always use Testcontainers with real PostgreSQL. Never use EF InMemory provider — it does not enforce schema-per-tenant migration logic.

**See:** `.claude/skills/testing.md` for full TDD patterns.

---

## Security Rules

- PAN, Aadhaar, bank account: encrypted at rest (AES-256, application layer). Never stored or logged in plaintext.
- Aadhaar: masked in all API responses (`XXXX-XXXX-1234`). Full reveal = authorised role + audit log entry.
- Every API endpoint: `[Authorize]` by default. Explicit `[AllowAnonymous]` with comment justifying it.
- Tenant isolation: JWT `tenant_id` claim must match subdomain on every request. Mismatch = 403, logged.
- No secrets in code or appsettings — Docker secrets + environment injection only.
- Input validation via FluentValidation in MediatR pipeline before any handler runs.
- Rate limiting on all auth endpoints.
- `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` headers via middleware.

**See:** `.claude/skills/security.md` for full patterns.

---

## Database Rules

- EF Core for writes + schema migrations only.
- Dapper for complex read queries, reports, bulk reads.
- Every migration: reversible `Up` + `Down`. Test rollback before merge.
- No raw SQL strings with user input — always parameterised queries.
- No `SaveChanges` in loops — batch writes.
- Soft deletes where audit trail matters (employees, salary structures, payroll runs).
- All timestamps: `timestamptz` (UTC). No `timestamp without time zone`.

**See:** `.claude/skills/database.md` for migration patterns.

---

## Git & Commit Standards

Conventional commits — mandatory:

```
feat(tds): add new regime slab computation for FY2026
fix(pf): correct EDLI cap enforcement for salary > 15000
test(engine): add edge cases for mid-month joiner proration
chore(deps): bump EF Core to 8.0.10
```

Scopes: `tds`, `pf`, `esi`, `pt`, `lwf`, `auth`, `tenant`, `payslip`, `form16`, `ecr`, `employee`, `engine`, `api`, `infra`, `deps`

- One logical change per commit.
- Never commit: secrets, `.env` files, `bin/`, `obj/`, `node_modules/`, migration snapshots with unreviewed changes.
- Branch naming: `feat/tds-slab-fy26`, `fix/pf-edli-cap`, `chore/upgrade-efcore`

---

## CI Gates (All Must Pass)

1. `dotnet build` — zero warnings
2. `dotnet test` — all tests green, coverage thresholds met
3. `dotnet format --verify-no-changes` — formatting enforced
4. `dotnet list package --vulnerable` — no known CVEs
5. NetArchTest — architecture dependency rules
6. `npm run lint` — ESLint zero errors
7. `npm run typecheck` — TypeScript zero errors
8. `vitest run --coverage` — coverage thresholds met
9. Playwright E2E — core flows green

---

## Payroll-Specific Invariants

- Payroll run is immutable once finalised. Never mutate a finalised payroll run — create a revision.
- Variable inputs file stored as immutable audit artifact per run.
- All statutory calculation results logged with inputs + outputs for audit.
- Reprocessing a payroll run must produce identical output given identical inputs (deterministic).
- Mid-year onboarding: YTD manual entry supported. Engine must accept prior-employer YTD as input.

---

## Skills Index

Load relevant skill before starting work in an area:

| Area | Skill file |
|---|---|
| Payroll engine (TDS/PF/ESI/PT/LWF calc) | `.claude/skills/payroll-engine.md` |
| .NET API + MediatR + FluentValidation | `.claude/skills/dotnet-api.md` |
| EF Core + Dapper + migrations | `.claude/skills/database.md` |
| React + TypeScript + Zod + RHF | `.claude/skills/react-frontend.md` |
| xUnit + Testcontainers + Vitest + Playwright | `.claude/skills/testing.md` |
| Auth + encryption + tenant isolation + RBAC | `.claude/skills/security.md` |
| Docker + Hangfire + Redis + MinIO + MailHog | `.claude/skills/infrastructure.md` |
