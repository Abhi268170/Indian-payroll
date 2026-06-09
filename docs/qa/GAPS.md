# GAPS.md — Items Not Testable Through the UI

Per mission: anything that cannot be exercised purely through the browser UI is recorded here with the reason, instead of being silently skipped. No `test.skip()` is used.

---

## UI-INACCESSIBLE SETUP

### G-1. SuperAdmin account is environment-seeded
- **What:** The only SuperAdmin (`admin@payroll.local`) is created by `SeedDataService` from `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD` env vars at API startup.
- **Why no UI:** No registration or user-creation UI produces a SuperAdmin; `CreateUserCommandValidator` explicitly forbids assigning the SuperAdmin role.
- **Impact on tests:** The test suite logs in with the seeded credentials read from the repo `.env`. This is the single unavoidable non-UI dependency to bootstrap everything else. Reading `.env` is a test-runner concern, not an app-API call.

### G-2. No Users / team-management UI — 4 of 6 roles uncreatable
- **What:** Roles `HRManager`, `PayrollManager`, `FinanceViewer`, `Employee` exist in the backend (`Roles.cs`, `UsersController` POST, OrgAdmin policy) but **no frontend route, nav entry, or page is wired to user creation** (verified: zero references to `/api/users` or those role strings in `web/src`).
- **Impact:** Only `SuperAdmin` (seeded) and `OrgAdmin` (created by org provisioning) are reachable through the UI. The full **permission matrix for the other 4 roles cannot be tested via the browser** — there is no UI to create such a user or log in as one. This is a product gap, not a test gap.
- **Tested instead:** SuperAdmin↔OrgAdmin boundary (route guards, redirects), anonymous deep-link blocks, suspended-tenant login block.

---

## EMAIL-GATED (resolved via inbox, documented)

### G-3. OrgAdmin first password is set only via an emailed link
- **What:** Provisioning creates an OrgAdmin with no password; the password is set only through a tokenised `/set-password` link delivered by email.
- **How handled:** The suite reads that link from **MailHog** (`http://localhost:8025`), the dev SMTP inbox. MailHog is the *email client* (the user's inbox), not the application backend — equivalent to a real user opening their email. It is the only non-app network call the tests make, and it is unavoidable by design. All other actions go through the app UI.

---

## TIME / RATE CONSTRAINED

### G-4. 1-hour password-reset token expiry
- **What:** Reset/set-password tokens expire after 1 hour (ASP.NET Identity default).
- **Why not tested:** A fast UI run cannot advance the wall clock an hour, and there is no UI control to expire a token. Would require clock manipulation or a 1-hour wait. **Not tested.**

### G-5. Auth rate limiter (5 requests / 60s / IP), observability
- **What:** `/connect/token` and `/api/auth/*` share a fixed-window limiter (5 permits / 60s / IP, `Program.cs`).
- **Impact on suite:** Login-heavy testing is throttled; the suite paces auth-calling specs across windows and relies on saved `storageState` so non-auth specs make zero auth calls. The 6th rapid auth call returns a throttle response, but the **UI surfaces it identically to a credential failure** ("Invalid credentials or server error."), so the limiter is not independently observable from the UI as a distinct state. Flagged rather than asserted as a distinct user-facing state.

---

## SCOPE NOTES
- Old tax regime: intentionally not implemented (v1 new-regime only) — nothing to test.
- displayName length divergence (FE max 200 / BE max 100) and password-policy divergence (FE min 8 / BE min 12) ARE testable and are covered as findings in RESULTS.md.

---

## ROUND 2 ADDITIONS

### G-6. Employee XLSX import — validate→commit happy path
- The empty downloaded template yields zero rows, so the full import (validate counts → commit → employees added) needs a POPULATED .xlsx fixture. e2e has no xlsx-writer dependency. Logo upload, template download, and the import dropzone/overwrite UI ARE covered. Filled-import commit deferred until a fixture generator is added.

### G-7. FnF settlement amounts / zero-value removal (WI-30)
- Exit initiation submit is blocked (RESULTS F-3: generic "Failed to initiate exit" even with a Tax Deductor assigned), so the FnF settlement inputs (bonus/commission/leave-encashment/gratuity/notice-pay) and zero-value removal could not be exercised end-to-end. The exit FORM (fields, validation, Proceed enablement, Active-only visibility) IS verified. Reaching the FnF page requires resolving the unnamed backend precondition behind F-3.

### G-8. Payslip PDF immediately post-payment
- Payslip generation is asynchronous after Record Payment; the PDF download is best-effort in payrun-state.spec (may not be ready within the test window). Bank Advice (.xlsx) download IS verified.
