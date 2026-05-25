# Onboarding Wizard — Plan (2026-05-24)

> ## ⚠️ SUPERSEDED by plan 014 (`2026-05-25-014-onboarding-ux-redesign-plan.md`)
>
> This plan shipped (Phases A–D, merged to master) but the resulting full-screen
> hard-redirect wizard tested poorly against Zoho Payroll's dashboard-checklist
> pattern. Plan 014 replaced the wizard with a `SetupChecklistCard` on the
> Dashboard, dropped the `/onboarding*` routes (Phase 4, commit ca3eb60+),
> and kept the backend status/preflight/seed-defaults endpoints from this plan.
>
> **What still applies from this plan:**
> - `GET /api/v1/onboarding/status` (+ `subSteps` extension in plan 014 Phase 2)
> - `GET /api/v1/payroll-runs/preflight`
> - `POST /api/v1/onboarding/seed-defaults/{step}`
> - `TenantSchemaProvisioner` seeding `OrgProfile` + `StatutoryOrgConfig`
> - Validator tightening (`FathersName`, bank fields)
> - `Employee.RecomputeProfileComplete` wiring
> - SuperAdmin route guard (`RequireTenantUser`)
>
> **What was removed by plan 014:**
> - `/onboarding` and `/onboarding/:stepId` routes
> - `OnboardingWizardPage.tsx`
> - `OnboardingAwareRedirect` (RootRedirect now goes straight to `/dashboard`)
> - `RequireNavGate` + sidebar lock icons (per-action gates replaced them)
>
> Read plan 014 for the current UX shape.

**Branch:** `onboarding` (from `master @ 29b5092`)
**Author:** Claude (Opus 4.7) under abhijith.sa
**Status:** Plan only — no code yet. **Revision 2** (2026-05-24): incorporates user's review findings (preflight readiness vs 422, field-based completeness instead of `ProfileComplete`, lightweight embedded employee step instead of mounting `AddEmployeeWizard`, explicit existing-tenant backfill, lock-on-Paid not Approved, validator scope expanded to Create + Update + Import).

## 1. Problem

A newly-provisioned tenant lands on a dashboard with no guidance, missing critical configs, and a "Pay Runs" button that explodes with vague backend errors when prerequisites are missing. The audit (`docs/ba-audit/audit-reports/2026-05-24-e2e-audit-report.md`) documented:

- DASH-001 empty dashboard with no next step
- TAX-001 Tax Details all empty, no banner forcing completion
- EMP-001 Active employees with incomplete profiles silently get skipped at payroll time
- PS-001 Pay Schedule lock state visually unclear (becomes immutable after first run)
- Generic backend errors like "Pay Schedule not configured" / "Statutory configuration not found" surfacing at `POST /api/v1/payroll-runs/initiate`

This plan introduces a guided onboarding wizard, dependency-aware navigation gating, and a server-side setup-status endpoint that drives both surfaces.

## 2. Goals

1. A newly-provisioned admin lands on `/onboarding` immediately after setting their password and never sees an empty dashboard.
2. A user cannot reach Pay Runs until the system has enough configuration to actually run a payroll (Pay Schedule + Statutory + 1 complete employee + salary structure).
3. Configuration order matches real dependency order — the user is never asked to pick a Department before any Designation exists, or assign a salary structure before any structure exists.
4. Statutory defaults follow Indian law out of the box (EPF wage cap, state-derived PT / LWF) so a default tenant is compliant unless they explicitly opt out.
5. Existing settings pages and CRUD endpoints are reused as-is — no duplicate forms.

## 3. Scope decisions (resolved with user)

| Question | Decision |
|---|---|
| Gating model | **Hard redirect to `/onboarding`** while incomplete. Sidebar shows the wizard only; Settings remains reachable for editing; People + Pay Runs hidden until minimum setup done. |
| Pay Schedule timing | **Early step + prominent immutability warning** ("Cannot change after first payroll runs"). |
| Father's Name + bank-account validator mismatch | **Tighten backend validators** to match what the engine actually requires. Mark as a sub-task inside this plan. |
| Statutory defaults | **Default ON with compliance defaults** — EPF enabled with wage cap on, PT/LWF derived from each work location's state, ESI applied per the seeded wage limits. Wizard surfaces toggles to opt out. |

## 4. Research summary

(Source: parallel codebase sweep, captured below as ground truth.)

### 4.1 What is auto-seeded on tenant provision
`src/Payroll.Infrastructure/Services/TenantSchemaProvisioner.cs:17-385`:
- The schema itself + EF migrations.
- 15+ salary components (Basic, HRA, LTA, Conveyance, Statutory Bonus, etc.) — 13 active, 7 inactive.
- PT slabs for 8 states, LWF state configs for 10 states.
- Income Tax slabs for FY 2026-27 + 2027-28, surcharge slabs, IT config (standard deduction, 87A rebate, PF wage cap ₹15k, EPF/ESI rates).

### 4.2 What is NOT auto-seeded
The following entities are required but never created automatically; today the user discovers they are missing only when they hit an error:
- `OrgProfile` (company name + statutory IDs).
- `StatutoryOrgConfig` — **disputed**: one research agent says auto-created with defaults, the other says it's missing. **Verify in implementation phase.**
- `PaySchedule`.
- `WorkLocation`, `Department`, `Designation`.
- `SalaryStructureTemplate`.
- Any `Employee`.
- `OrgProfile.DeductorEmployeeId`.

### 4.3 Hard blockers to `POST /api/v1/payroll-runs/initiate`
From `src/Payroll.Application/Commands/PayrollRuns/InitiatePayrollRunCommand.cs`:
- L48–49: `PaySchedule` missing → `"Pay Schedule not configured."`
- L75–76: `StatutoryOrgConfig` missing → `"Statutory configuration not found."`
- L83–198: At least one active employee — and per-employee silent skip if any of: `DateOfBirth`, `FathersName`, `EncryptedBankAccount`, active salary structure is missing.
- L95–96, L114–130: Work locations needed to map PT + LWF per employee.
- L78–81: Income Tax config for the fiscal year (already seeded).

### 4.4 Hard blockers to `POST /api/v1/employees/{id}/initiate-exit`
From `src/Payroll.Application/Commands/Employees/InitiateExitCommand.cs`:
- L80–84: If the exiting employee IS the org's `DeductorEmployeeId`, exit is blocked.
- L87–88: PaySchedule needed for FnF pay-date computation.
- StatutoryConfig + employee work location needed.

### 4.5 Existing patterns we can reuse
- `web/src/pages/employees/AddEmployeeWizard.tsx` — step-based multi-page form with react-hook-form + zod. Same shape can drive the org-level wizard.
- `web/src/pages/employees/EmployeesPage.tsx:75` — incomplete-profile banner pattern, reusable as a setup-status banner if we ever soften the gating later.
- `web/src/router.tsx:52-60` — `RootRedirect` is the right place to insert the "is setup complete?" check.
- Existing CRUD endpoints for every entity below — the wizard wraps them, doesn't replace them.

### 4.6 Validator / engine drift to fix (audit follow-up)
**Scope expanded after review** — fixes must cover the create path, the update path, AND the bulk-import path so the gap closes end-to-end.

- `FathersName`
  - Engine treats it as a blocker: `InitiatePayrollRunCommand.cs:193-194` skips employees with empty `FathersName`.
  - Frontend zod requires it: `WizardStep3Personal.tsx:26`.
  - Backend create validator does NOT require it: `CreateEmployeeCommand.cs:31` (per review).
  - Backend update + import paths also do not require it.
  - **Fix in all three places** (create, update-personal, bulk-import row validator) so an employee row can never be persisted without it.

- Bank fields (`AccountHolderName`, `BankName`, `AccountType`, `IFSC`)
  - Backend `UpdatePaymentInfoCommand.cs:29` conditionally requires them when `PaymentMode = BankTransfer`.
  - Frontend `WizardStep4Payment.tsx:22` only marks `AccountNumber` as conditional.
  - **Fix on frontend** (Step 4 zod) AND on bulk-import row validator so all three entry paths converge.

### 4.7 `Employee.ProfileComplete` is a dead flag today
`Employee.cs:207` defines `ProfileComplete` (defaults `false`) and `MarkProfileComplete()` exists, but the application layer has **zero callers** — search of `src/Payroll.Application/` shows no mutation path. As a result the flag is permanently false in production, even for fully-populated employees. The audit's "incomplete profile" banner on the Employees list works only because the API derives the flag at read-time from `EmployeeListItemDto` (verify in implementation phase).

Implication for this plan: the wizard cannot gate on `ProfileComplete = true` without first wiring up completion semantics. **§5.4 has been rewritten to use field-presence checks instead** of relying on the flag. Wiring `MarkProfileComplete` correctly is a separate post-Phase-A workstream — out of scope for this plan, tracked as **OQ-5** in §13.

### 4.8 Async-initiate API contract precludes a synchronous 422
`POST /api/v1/payroll-runs/initiate` returns **202 Accepted** with a Hangfire job id (`PayrollRunsController.cs:28-34`). The handler runs asynchronously inside the worker, so handler exceptions never produce an immediate HTTP 422 to the operator's request. The plan must NOT promise "structured 422 on initiate" — that was wrong in Revision 1.

Implication: readiness must be checked **before** the operator clicks Process Payroll, via a new synchronous `GET /api/v1/payroll-runs/preflight` endpoint (see §7.2). The Process Payroll button stays disabled while preflight reports unmet prerequisites.

### 4.9 Pay Schedule lock fires on Paid, not Approved
`RecordPaymentCommand.cs:55` is the only place that sets `PaySchedule.IsLockedAfterPayrun = true`. Approval alone does not lock — only payment recording does. Revision 1 of this plan said "Approved or Paid" which is wrong. Onboarding-status query and UI must mirror this exactly (`locked = exists PayrollRun with Status = Paid`).

### 4.10 `AddEmployeeWizard` is route-driven, not embeddable
`AddEmployeeWizard.tsx:16,27` uses `useParams<{ id, step }>()` and `useNavigate()` internally to walk through `/employees/:id/wizard/:step`. Mounting it inline in the onboarding wizard (as Revision 1 proposed) would either hijack the URL or break its internal navigation.

Implication: **§5.3 step 9 is rewritten** to use a slim onboarding-specific `FirstEmployeeStep` that reuses the existing zod schemas + form sub-components from `AddEmployeeWizard` but owns its own state and posts to the same employee CRUD endpoints. Future refactor to make `AddEmployeeWizard` accept an embedded-mode prop is a separate workstream (tracked as **OQ-6** in §13).

## 5. UX design

### 5.1 Entry points
1. **From SetPasswordPage success** → if the tenant is freshly provisioned (no setup ever run), the post-success CTA changes from "Go to Login" to a one-click sign-in that lands the user on `/onboarding`.
2. **From RootRedirect** → if the user has `role !== SuperAdmin` AND `GET /api/v1/onboarding/status → setupComplete === false`, redirect to `/onboarding` instead of `/dashboard`.
3. **Direct navigation** to `/dashboard`, `/employees`, `/pay-runs` while incomplete also redirects to `/onboarding`.
4. The user can always reach `/settings/*` from inside `/onboarding` (deep-link "edit existing value" buttons).

### 5.2 Layout
```
+----------------------------------------------------------+
| Setup your organisation                                  |
|  Progress: 3 / 9 steps complete                          |
+----------------------------+-----------------------------+
| 1. Organisation Profile ✓  |  [Step content panel]       |
| 2. Tax Details         ✓   |  Form for the active step,  |
| 3. Work Locations      ●   |  Save & Continue, Skip,     |
| 4. Departments + Roles     |  Back, links to deep edits  |
| 5. Pay Schedule  ⚠ lock    |                             |
| 6. Statutory               |                             |
| 7. Salary Structure        |                             |
| 8. Tax Deductor Employee   |                             |
| 9. Add first employee      |                             |
| ------------------------   |                             |
| Skip setup → dashboard     |  Footer: Save & Continue    |
+----------------------------+-----------------------------+
```
- Left-rail step list with three states: completed (✓), active (●), pending (empty).
- "Skip setup" is **hidden** until the minimum-payroll prerequisites (§6) are met — once those are satisfied the user can leave the wizard whenever they want and the redirect stops firing.
- Step 5 (Pay Schedule) carries a permanent ⚠ icon + tooltip "This becomes immutable after the first payroll run."

### 5.3 Step-by-step spec

Each step renders a form that posts to an existing endpoint. The wizard owns step state + nav only — not the actual entity logic.

| # | Step | Backing endpoint(s) | Minimum fields | "Skip" behaviour |
|---|---|---|---|---|
| 1 | Organisation Profile | `PUT /api/v1/org-profile` | Company name. PAN, GSTIN, address optional. | Cannot skip — `OrgProfile.CompanyName` is required everywhere. |
| 2 | Tax Details (TAN + AO + Deductor type/name) | `PUT /api/v1/org-profile/tax-details` | All optional today. Wizard recommends TAN. | Skippable. Note: blocks Form 24Q / Form 16 if skipped. |
| 3 | Work Locations | `POST /api/v1/work-locations` | At least one location with state. | Cannot skip — engine needs state to load PT/LWF. Pre-populate name "Head Office" with org-profile state if present. |
| 4 | Departments + Designations | `POST /api/v1/departments`, `POST /api/v1/designations` | At least one department, at least one designation. | Cannot skip — needed for adding any employee. Default suggestions: "Engineering", "Operations" + "Software Engineer", "Manager". |
| 5 | Pay Schedule | `PUT /api/v1/pay-schedule` | Work week days, calculation method, pay date type, pay date day if SpecificDay. First-pay-period month + year recommended. | Cannot skip. **Banner**: "After your first payroll run, work week and salary calc method become immutable." |
| 6 | Statutory (EPF, ESI, PT, LWF, Statutory Bonus) | `PUT /api/v1/statutory/epf`, `…/esi`, etc. | Confirm EPF establishment code if EPF on; rest are toggles. | Cannot skip — engine reads `StatutoryOrgConfig`. **Defaults applied here**: EPF on with wage cap, statutory bonus on, PT/LWF derived from work-location states. |
| 7 | Salary Structure | `POST /api/v1/salary-structure-templates` | At least one template "Standard" with the seeded earnings + a residual Fixed Allowance. | Skippable in the wizard UX, but cannot add an employee in step 9 without one. We surface a "Use suggested standard" one-click that creates a sensible default and continues. |
| 8 | Tax Deductor Employee | `PUT /api/v1/org-profile/tax-details` (only the `deductorEmployeeId` field, see audit `TAX-003`) | Pick an employee that will sign Form 16. Only enabled after step 9 creates at least one employee. | Skippable, but exits will be blocked for the chosen deductor (see §4.4). |
| 9 | Add your first employee | **New `FirstEmployeeStep` component (NOT `AddEmployeeWizard` inline)** — owns its own state, reuses Step 1 + Step 3 + Step 4 zod schemas + form sub-components from `AddEmployeeWizard`. Posts to the same `POST /api/v1/employees`, `PUT /api/v1/employees/{id}/personal`, `PUT /api/v1/employees/{id}/payment`, `POST /api/v1/employees/{id}/salary-structure` endpoints. See §4.10 for why we cannot mount the existing wizard directly. | Step 1 + Step 3 + Step 4 + assigned salary structure (concrete fields, see §5.4). | Cannot skip to "complete" — needs at least one ready-for-payroll employee. User can skip the *whole wizard* though (see §5.2). |

Step 8 is intentionally placed after step 9 in the visible order because it depends on an employee existing. The wizard auto-jumps back to 8 the moment step 9 has a saved employee.

### 5.4 "Setup complete" trigger — field-based, not flag-based
Because `Employee.ProfileComplete` has no production write path today (§4.7), the wizard computes completeness from concrete persisted fields. The status query (§7.1) returns `setupComplete = true` when ALL of the following hold:

- `OrgProfile.CompanyName` non-empty.
- `WorkLocation` count ≥ 1.
- `Department` count ≥ 1.
- `Designation` count ≥ 1.
- `PaySchedule` row exists with required fields populated.
- `StatutoryOrgConfig` row exists (after Phase A this is auto-seeded on provision, so always true).
- `SalaryStructureTemplate` count ≥ 1.
- At least one `Employee` where ALL of the following are populated (the exact engine blockers from `InitiatePayrollRunCommand.cs:193-195`):
  - `DateOfBirth` non-null
  - `FathersName` non-empty
  - `EncryptedBankAccount` non-empty
  - Has an active `EmployeeSalaryStructure` row

Step 2 (Tax Details) and step 8 (Deductor) are not blockers — they're surfaced as warnings on the dashboard and Pay Runs page if missing, but do not gate navigation. Rationale: a tenant can run payroll even without TAN; they only need it to file Form 24Q.

**Note on `ProfileComplete`:** wiring `MarkProfileComplete()` callers is tracked as **OQ-5** in §13 — once that exists, the status query can switch to reading the flag instead of duplicating the field check. The field-presence definition above is the authoritative one until then.

## 6. Navigation gating

Two distinct concepts:
1. **Wizard redirect** (above) — controlled by `setupComplete`.
2. **Per-nav-item gating** — even after the user exits the wizard, the People and Pay Runs nav items remain disabled until their own prerequisites are met. This is the safety net for users who skip individual wizard steps via deep-linked Settings.

| Nav item | Enabled when |
|---|---|
| Dashboard | always |
| Settings | always |
| People | `Departments ≥ 1` AND `Designations ≥ 1` AND `WorkLocations ≥ 1` AND `SalaryStructureTemplates ≥ 1` |
| Pay Runs | People enabled AND `PaySchedule` configured AND `StatutoryOrgConfig` configured AND `Employees with profileComplete=true ≥ 1` |

Disabled items render with tooltip explaining what's missing + a "Go to Settings → Pay Schedule" deep link.

## 7. Backend

### 7.1 New query: `GET /api/v1/onboarding/status`
Single endpoint that drives both the wizard left rail AND the nav gating. Returns:
```jsonc
{
  "setupComplete": false,
  "steps": [
    { "id": "org-profile",        "complete": true,  "required": true,  "skippable": false },
    { "id": "tax-details",        "complete": false, "required": false, "skippable": true  },
    { "id": "work-locations",     "complete": false, "required": true,  "skippable": false, "count": 0 },
    { "id": "org-structure",      "complete": false, "required": true,  "skippable": false, "deptCount": 0, "desigCount": 0 },
    { "id": "pay-schedule",       "complete": false, "required": true,  "skippable": false, "locked": false },
    { "id": "statutory",          "complete": false, "required": true,  "skippable": false },
    { "id": "salary-structure",   "complete": false, "required": true,  "skippable": false, "templateCount": 0 },
    { "id": "deductor-employee",  "complete": false, "required": false, "skippable": true,  "blockedBy": "first-employee" },
    { "id": "first-employee",     "complete": false, "required": true,  "skippable": false, "completeCount": 0 }
  ],
  "navGates": {
    "people":   { "enabled": false, "missing": ["work-locations", "departments", "designations", "salary-structure"] },
    "payRuns":  { "enabled": false, "missing": ["pay-schedule", "statutory", "first-employee"] }
  }
}
```
Pure read: no writes, no mutations. Cached on the client via TanStack Query; invalidated whenever any settings mutation completes (`queryClient.invalidateQueries({ queryKey: ['onboarding-status'] })`).

Handler responsibilities (`GetOnboardingStatusHandler`):
- Probe each entity via existing repositories. No new DB queries for the wizard's sake — reuse what exists.
- `org-profile` complete = `OrgProfile.CompanyName` non-empty.
- `work-locations` complete = `count ≥ 1`.
- `org-structure` complete = `Departments ≥ 1 AND Designations ≥ 1`.
- `pay-schedule` complete = `PaySchedule` exists; `locked = exists PayrollRun WHERE Status = Paid` (NOT Approved — see §4.9). Maps to `PaySchedule.IsLockedAfterPayrun` if present.
- `statutory` complete = `StatutoryOrgConfig` row exists. (After Phase A this is auto-seeded both on provision AND backfilled for existing tenants — see §7.4.)
- `salary-structure` complete = at least one `SalaryStructureTemplate`.
- `first-employee` complete = exists at least one `Employee` matching the field-presence check in §5.4 (NOT `ProfileComplete = true` — see §4.7).
- `deductor-employee` complete = `OrgProfile.DeductorEmployeeId` non-null AND points at an existing active employee.

### 7.2 Preflight readiness endpoint (replaces "structured 422 on initiate")
`POST /api/v1/payroll-runs/initiate` is async (returns 202 + job id, runs in Hangfire — see §4.8), so it cannot return an inline 422 to the operator. Instead:

- **New endpoint**: `GET /api/v1/payroll-runs/preflight` — synchronous, returns the same readiness payload shape as `/onboarding/status` but scoped to "can we run a payroll right now?":
  ```jsonc
  {
    "ready": false,
    "blockers": [
      { "code": "PAY_SCHEDULE_MISSING",     "message": "Pay Schedule not configured.",          "fixUrl": "/settings/pay-schedule" },
      { "code": "STATUTORY_MISSING",        "message": "Statutory configuration not found.",    "fixUrl": "/settings/statutory" },
      { "code": "NO_PAYABLE_EMPLOYEES",     "message": "No employees with payroll-ready profile.", "fixUrl": "/employees", "count": 0 }
    ],
    "warnings": [
      { "code": "TAX_DETAILS_INCOMPLETE",   "message": "Form 24Q + Form 16 will be unavailable until Tax Details are set.", "fixUrl": "/settings/tax-details" }
    ]
  }
  ```
- The **Process Payroll** button on the Pay Runs page reads this endpoint on mount and stays disabled while `ready === false`. Hovering the disabled button shows the blocker list inline.
- Optional bonus: a future server-side guard inside `InitiatePayrollRunHandler` may still surface blockers in the job-failure payload (already returned via `/api/v1/jobs/{jobId}/status`) for the case where state changed between preflight and click. Out of scope for Phase A.

### 7.3 Tighten validators across all entry paths (review finding #6)
**Scope:** every place an employee row is created or updated — Create, Update-Personal, Update-Payment, and the bulk-import row validator inside `BulkImportBackgroundJob`.

- `FathersName` required:
  - `CreateEmployeeCommandValidator` (add `.NotEmpty().MaximumLength(...)`)
  - `UpdateEmployeePersonalCommandValidator` (already validates other personal fields — add same)
  - Bulk-import row validator for employees (whatever runs inside the importer pipeline)
- Bank fields conditional on `PaymentMode = BankTransfer`:
  - Backend `UpdatePaymentInfoCommand.cs:29` is already correct.
  - **Frontend** `WizardStep4Payment.tsx:22` zod must conditionally require `AccountHolderName`, `BankName`, `AccountType`, `IFSC` when `PaymentMode = BankTransfer`.
  - Bulk-import row validator: same conditional requirement.
- Add tests for each path so a regression cannot re-open the gap.

### 7.4 Compliance defaults — provision AND backfill (review finding #4)
`TenantSchemaProvisioner.cs:49` runs migrations + seeds salary components + statutory slabs but DOES NOT create a `StatutoryOrgConfig` row. `StatutoryComponentsController.cs:21` masks this today by returning a fallback DTO with everything disabled — so the UI shows "EPF disabled" for every fresh tenant. After Phase A:

**For new tenants** (provision path):
- Extend `TenantSchemaProvisioner` to seed:
  - `OrgProfile` row with `CompanyName = <Tenant display name>` (overwritten in step 1).
  - `StatutoryOrgConfig` row with compliance defaults: `EpfEnabled = true`, `EpfEmployeeContributionRate = RestrictedWage12` (₹15k cap), `EpfEmployerContributionRate = RestrictedWage12`, `EsiEnabled = true`, `StatutoryBonusEnabled = true`, `GratuityIncludedInCtc = true`.
- No PaySchedule, no Salary Structure, no Employees — those are deliberately wizard-driven.

**For existing tenants** (backfill path — new in Revision 2):
- **One-off backfill job** (runnable as `dotnet run --project src/Payroll.Api -- backfill-org-defaults` or a Hangfire one-shot at startup) that iterates every tenant schema and, if either `OrgProfile` or `StatutoryOrgConfig` is missing, inserts the same defaults. Idempotent — re-run safe.
- Backfill must:
  - Run inside each tenant schema (use `TenantSchemaProvisioner`'s search-path setup).
  - Skip tenants where the row already exists (no overwrites).
  - Log a summary per tenant: created vs already-present.
  - Be reversible: a `--dry-run` flag prints what it would do without writing.
- After backfill, `StatutoryComponentsController.cs:21` should be tightened to fail (404) if the row is missing rather than returning a fallback DTO. That makes the contract honest going forward.

This resolves the disputed point in §4.2 (Revision 1) and means the engine never errors with "Statutory configuration not found" on any tenant, new or existing.

### 7.5 No new domain entities
Setup state is computed, never stored. No new migrations for the wizard itself. Validator-tightening sub-tasks may add `[Required]` annotations but no schema changes. The backfill is a one-off job, not a migration.

## 8. Frontend

### 8.1 New routes
```
/onboarding                  → OnboardingWizardPage (default redirect when setup incomplete)
/onboarding/:stepId          → same page, deep-link to a specific step
```

### 8.2 New files
- `web/src/pages/onboarding/OnboardingWizardPage.tsx` — shell + step navigation.
- `web/src/pages/onboarding/steps/OrgProfileStep.tsx` — wraps existing OrgProfile form fields.
- …same pattern for each step: `TaxDetailsStep`, `WorkLocationsStep`, `OrgStructureStep`, `PayScheduleStep`, `StatutoryStep`, `SalaryStructureStep`, `DeductorEmployeeStep`, `FirstEmployeeStep`.
- `web/src/components/onboarding/StepRail.tsx` — left-rail list of steps.
- `web/src/components/onboarding/Stepper.tsx` — progress + Back/Next/Skip controls.
- `web/src/components/nav/NavGateTooltip.tsx` — disabled sidebar item with explanation tooltip.
- `web/src/hooks/useOnboardingStatus.ts` — single source of truth, calls `GET /api/v1/onboarding/status`.

### 8.3 Reuse, don't reinvent
- `FirstEmployeeStep` is a **slim, onboarding-specific component**. It does NOT mount `AddEmployeeWizard` directly because that wizard is route-driven (`/employees/:id/wizard/:step`) and would hijack URL state — see §4.10. Instead, `FirstEmployeeStep` imports and reuses:
  - The same zod schemas (export from `WizardStep1Basic.tsx`, `WizardStep3Personal.tsx`, `WizardStep4Payment.tsx`).
  - The same form sub-components where they're already extracted; lifts them out if they aren't.
  - The same backend endpoints (`POST /api/v1/employees`, `PUT /api/v1/employees/{id}/personal`, `PUT /api/v1/employees/{id}/payment`, `POST /api/v1/employees/{id}/salary-structure`).
- Each step embeds the existing settings form components or reproduces them with the same react-hook-form + zod schemas. No duplicated business validation.
- The `Process Payroll` button on the Pay Runs page hooks into `GET /api/v1/payroll-runs/preflight` (§7.2) for disable/enable state + blocker tooltip.

### 8.4 RootRedirect logic update
`web/src/router.tsx:52-60`:
```tsx
function RootRedirect(): React.ReactElement {
  const user = useAuthStore(s => s.user)
  const roles = Array.isArray(user.role) ? user.role : [user.role]
  if (roles.includes('SuperAdmin')) return <Navigate to="/platform/orgs" replace />
  const { data: status } = useOnboardingStatus()           // suspense or skeleton-aware
  if (status && !status.setupComplete) return <Navigate to="/onboarding" replace />
  return <Navigate to="/dashboard" replace />
}
```
Plus a route guard on `/employees`, `/pay-runs` that redirects to `/onboarding` while `!setupComplete`.

### 8.5 Sidebar
`web/src/components/AppLayout.tsx` (or wherever the sidebar lives) reads `status.navGates` and renders disabled items with a tooltip listing the missing prerequisites.

### 8.6 Dashboard banner
While `setupComplete === true` but `tax-details` or `deductor-employee` are incomplete, the dashboard shows a non-blocking yellow banner: "Tax Details incomplete — Form 24Q + Form 16 will be unavailable. Configure now."

## 9. Test plan

Backend:
- `GetOnboardingStatusHandler` returns each step's `complete` flag correctly for a freshly seeded tenant, a half-configured tenant, and a fully configured tenant. Specifically: `first-employee.complete` flips true based on the field-presence check in §5.4, NOT on `ProfileComplete`.
- `PaySchedule.locked` flips only after a `PayrollRun` reaches `Status = Paid`, NOT on Approve (mirrors `RecordPaymentCommand.cs:55`).
- `StatutoryOrgConfig` default is seeded at tenant provision AND the backfill job creates it for any pre-existing tenant.
- `CreateEmployeeCommandValidator`, `UpdateEmployeePersonalCommandValidator`, and the bulk-import row validator all reject empty `FathersName`.
- `GET /api/v1/payroll-runs/preflight` returns `ready = true` exactly when the field-based check in §5.4 passes, with the right `blockers[]` / `warnings[]` payload otherwise.
- After Phase A, `GET /api/v1/statutory/config` returns the real seeded row and the controller-side fallback DTO is removed.

Frontend:
- New tenant logs in → lands on `/onboarding/org-profile`.
- Save Org Profile → step list updates, focus moves to step 2.
- Pay Schedule step shows lock warning.
- Pay Schedule lock state visible (read-only) after first payroll.
- People + Pay Runs nav items disabled with tooltips while gates fail; enabled the moment they pass.
- `AddEmployeeWizard` inside `FirstEmployeeStep` saves an employee and the step ticks complete only when `profileComplete === true`.
- Deep-link `/onboarding/statutory` jumps straight to step 6.
- `setupComplete === true` removes the redirect; user can navigate freely.

Manual:
- Provision a fresh tenant via /platform/orgs/new, follow wizard end-to-end, run first payroll, initiate an exit. No engine errors should reach the user.

## 10. Phasing

**Phase A — Backend foundations** (no UI yet):
- `GET /api/v1/onboarding/status` with field-based completeness check.
- `GET /api/v1/payroll-runs/preflight` (replaces the abandoned "422 on initiate" idea).
- `TenantSchemaProvisioner` seeds default `OrgProfile` + `StatutoryOrgConfig` on new tenants.
- **Backfill job** to seed defaults for all existing tenants. `--dry-run` flag supported. Idempotent.
- Validator tightening in **Create + Update + Import** entry paths: `FathersName` required, bank fields conditional on `BankTransfer` (frontend zod + import row validator).
- Tighten `GET /api/v1/statutory/config` to return real seeded row (drop the fallback DTO).

**Phase B — Wizard shell**:
- New routes, `RootRedirect` update, sidebar gating, dashboard banner.
- Step rail + skeleton step pages that just wrap existing settings forms.

**Phase C — Step polish**:
- "Use suggested standard" defaults (work location, salary structure, departments).
- Pay Schedule immutability warning.
- Tax Details + Deductor surfaced as soft warnings.

**Phase D — E2E + audit follow-ups**:
- Cypress / Playwright happy path.
- Audit doc cross-references closed (DASH-001, TAX-001, EMP-001, PS-001, plus new PROVISION-DEFAULTS).

Each phase is one PR off `onboarding`.

## 11. Sub-tasks (closing audit findings inside this scope)

- **DASH-001** → Phase B (real dashboard already shipped; redirect now hides it while incomplete).
- **TAX-001** → Phase C (Tax Details step + dashboard banner).
- **EMP-001** → Phase A (validator tightening + UI removes the "Active + Incomplete" silent skip).
- **PS-001** → Phase C (immutability warning + locked read-only render after first run).
- **PROV-DEFAULTS (new)** → Phase A (seed OrgProfile + StatutoryOrgConfig at provision).

## 12. Out of scope (explicitly)

- Reports / Documents / Forms / Loans / Approvals modules (audit NAV-001 — separate plans).
- Bulk employee import as a wizard step (covered by existing Import flow, link from step 9).
- Multi-admin invite during onboarding (Users & Roles module is not yet built).
- Payroll first-run guided experience (separate plan).
- Localised onboarding copy (i18n).

## 13. Open questions (lower-priority, do not block plan approval)

- **OQ-1**: When the user clicks "Skip setup" after meeting minimum requirements, do we persist that choice in a `tenant.onboarding_skipped_at` column (so dashboard banner doesn't keep nagging), or rely on client localStorage?
- **OQ-2**: Should step 8 (Deductor Employee) appear at all if Tax Details (step 2) was skipped? Currently we show it regardless — easier mental model.
- **OQ-3**: For multi-state orgs, do we want per-location PT/LWF override during the wizard, or accept the seeded defaults and surface overrides only inside Settings? Current plan: seeded defaults only; Settings handles overrides.
- **OQ-4**: Audit `TAX-003` (Deductor picker on Tax Details page) is already shipped. Should step 8 reuse that picker or be a richer "pick + preview Form-16 signature" experience? Current plan: reuse the picker, defer the preview.
- **OQ-5 (new in Revision 2)**: Wire up `Employee.MarkProfileComplete()` callers so the `ProfileComplete` flag actually flips in production. Once done, the §5.4 field-presence check can be replaced with a single boolean read. Tracked as a separate post-Phase-A workstream — when should it land? Recommendation: bundle into Phase D since it's a small change once the wizard exists to drive the call site.
- **OQ-6 (new in Revision 2)**: Refactor `AddEmployeeWizard` to accept an `embedded` prop (no URL hijack, parent-controlled navigation) so step 9 can mount it directly instead of duplicating sub-components. Defer until step 9's slim form proves stable. Tracked for a future cleanup PR.

## 14. Estimated effort (Revision 2)

- Phase A: ~**1.5 days** backend + tests (was 1 day; revised upward because of the preflight endpoint + tenant backfill job + validator scope expansion + tightening `/statutory/config` contract).
- Phase B: ~1 day FE shell + routing.
- Phase C: ~1.5 days FE polish + defaults + slim `FirstEmployeeStep` build (was 1.5 days; component build absorbed into existing budget).
- Phase D: 0.5 day E2E + optional `MarkProfileComplete` wiring (OQ-5).

Total: ~**4.5 dev-days** end-to-end. Each phase ships independently.

---

**Next action:** await plan approval, then start Phase A on this branch (`onboarding`).
