# Onboarding UX Redesign — Plan (2026-05-25)

**Branch:** TBD (start on master `b4703e8`)
**Status:** Plan only — no code yet. **Revision 2** (2026-05-25): incorporates review findings on pay-run deep-link protection, correct gate-signal for Add Employee, deterministic statutory sub-step rules, deprecation redirect for `/onboarding` URLs, and the welcome first-name fallback contract.
**Trigger:** Current onboarding is functional but feels heavy/blocking compared to Zoho Payroll's friendlier checklist surface.

## 1. Side-by-side: Zoho vs ours today

### Zoho (`payroll.zoho.in`)

- **Landing on first login:** the regular Dashboard, with confetti animation and `Welcome <name>!` headline.
- **Setup checklist lives ON the dashboard** as one expandable card titled "Get started with Zoho Payroll" with a big progress bar (`5/7 Completed`).
- **All steps numbered 1–7**, each row:
  - Green ✓ icon when complete, grey ○ when pending.
  - Step title is a hyperlink → opens the corresponding **real Settings page** (not a wizard panel).
  - Right-aligned `COMPLETED` (green) or `Complete Now` (blue pill button).
- **Sub-steps indented** under parent rows (Statutory: EPF / ESI / LWF / PT shown as nested checks).
- **Full sidebar nav remains visible and clickable** throughout setup. Nothing is gated. Employees / Pay Runs / Taxes & Forms / Loans / Giving / Reports — all reachable even on day 0.
- **"Additional notable features"** card below shows promotional tiles (Direct Deposit, Salary Templates, etc.).
- **Help block** at the bottom: walkthrough video + Help / FAQ / Forum + clarification email + toll-free phone number.
- **Settings opens in a modal-like overlay** with its own sidebar. `Close Settings` returns to wherever the user came from.
- **"Setup completion"** is informational — never blocks Pay Runs. Pay Runs page itself uses its own preflight before allowing initiation.

### Ours today

- **Landing on first login:** hard redirect to `/onboarding`, a full-screen wizard. Dashboard hidden.
- **Wizard layout:** left rail of 9 steps + step content panel on the right.
- **Sidebar disabled / nav gated** until prerequisites are met (People needs depts + desigs + work-locations + salary-structure; Pay Runs adds pay-schedule + statutory + 1 payroll-ready employee).
- **Each step panel** shows: title + Required/Optional/Complete pill + helper text + "Apply suggested defaults" CTA (where applicable) + "Open <step> settings" link.
- **"Open settings"** opens in the **same tab** with a `?return=onboarding&step=…` banner; clicking around in the settings sidebar preserves the banner.
- **No welcome moment**, no celebration animation, no progress percent visualisation, no sub-step breakdown.
- **Hard redirect** means a user who wants to peek at the Employees page can't until they finish minimum config.

## 2. UX delta — what feels worse

| # | Issue | Zoho behaviour | Our behaviour |
|---|---|---|---|
| 1 | First-screen friction | Familiar dashboard with checklist | Full takeover wizard, no other surface visible |
| 2 | Sense of progress | Big "5/7 Completed" bar + numbered steps | Tiny "0/7 required steps complete" subtitle in header |
| 3 | Sub-step visibility | Statutory breaks into EPF/ESI/LWF/PT visible | Single "Statutory" row, opens settings tab to discover sub-tabs |
| 4 | Step navigation | Each step is a hyperlink to existing Settings page | Each step is a wizard panel that *also* links to Settings |
| 5 | Optionality of nav | All sidebar items reachable | People + Pay Runs locked (lock icon + tooltip) |
| 6 | Welcome warmth | Confetti + `Welcome <name>!` + webinar pitch | Plain `Set up your organisation` header |
| 7 | Help discoverability | Help/FAQ/Forum tiles + phone + email + walkthrough video on the dashboard | None |
| 8 | "Setup complete" terminology | Never says "setup complete" — it's an evolving checklist | Switches mode entirely (wizard → dashboard) at a hard boundary |
| 9 | First payroll discoverability | Step 7 = "Configure Prior Payroll", step 6 = "Add Employees" — payroll itself is *not* a wizard step | Step 9 in our wizard is "Add First Employee" + Pay Runs is gated behind setup-complete |
| 10 | Promotional tiles | "Direct Deposit / Salary Templates / Auto IT Reminder / Custom Field" → upsell + feature discovery | Nothing |

## 3. Goals for the redesign

1. **Stop the hard redirect.** New tenants land on the Dashboard. The dashboard renders a checklist card alongside whatever KPIs we have.
2. **Keep the safety net.** Pay Runs preflight already blocks the dangerous action; we don't need to gate the nav.
3. **Surface sub-steps** (Statutory → EPF / ESI / PT / LWF) so users see what "Statutory" actually contains.
4. **Reuse the existing Settings pages.** Step click navigates to Settings — same pattern Zoho uses — no second form layer.
5. **Add welcome warmth** without childish confetti: a celebratory but tasteful "Welcome to <Company>" with the admin's first name + a one-line "Let's get you ready for your first payroll."
6. **Surface help** prominently on the dashboard (docs links + MailHog in dev).

## 4. Proposed UX (concrete)

### 4.1 Dashboard layout (incomplete tenant)

```
┌─ Top bar ────────────────────────────────────────────────────────┐
│ Logo                                          Smoke Test Corp ▾  │
├──────────────────────────────────────────────────────────────────┤
│  Welcome to Smoke Test Corp, Asha 👋                             │
│  Let's get you ready for your first payroll run.                 │
│                                                                  │
│  ┌─ Get Started ────────────────────────────────────  3 / 9 ✓ ─┐ │
│  │ ▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  │ │
│  │                                                             │ │
│  │ ✓ 1. Organisation Profile                          COMPLETED│ │
│  │ ✓ 2. Tax Details                                   COMPLETED│ │
│  │ ✓ 3. Work Locations                                COMPLETED│ │
│  │ ○ 4. Departments & Designations          ►   Complete Now   │ │
│  │ ⚠ 5. Pay Schedule (locks after 1st run)  ►   Complete Now   │ │
│  │ ○ 6. Statutory Components                ►   Complete Now   │ │
│  │       ✓ Provident Fund                                      │ │
│  │       ✓ State Insurance                                     │ │
│  │       ○ Professional Tax (per work location)                │ │
│  │       ○ Labour Welfare Fund                                 │ │
│  │ ○ 7. Salary Structure                    ►   Complete Now   │ │
│  │ ○ 8. Tax Deductor Employee (after step 9)                   │ │
│  │ ○ 9. Add Your First Employee             ►   Add Employee   │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌─ KPI tiles (rendered greyed-out until each is populated) ────┐│
│  │ Active Employees  Current Period  Pay Run Status  Last Run  ││
│  │       0                Apr 2026          —              —   ││
│  └──────────────────────────────────────────────────────────────┘│
│                                                                  │
│  ┌─ Need help? ────────────────────────────────────────────────┐ │
│  │ 📖 Walkthrough video    📞 Support    💬 Forum   📧 Email   │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### 4.2 Step row behaviour

- **Pending step row:** Grey ○ + step title + chevron + blue `Complete Now` pill (right-aligned).
- **In progress / partial:** Amber half-circle + green sub-step ticks underneath where applicable.
- **Complete:** Green ✓ + green `COMPLETED` label (right-aligned, no pill).
- **Click anywhere on row** → opens corresponding Settings page in the same tab (with `?return=dashboard` so SettingsLayout's "Back to setup" button returns to `/dashboard`).
- **Sub-step rows** are read-only — clicking parent jumps to settings page where they're tabs.
- **Pay Schedule row** has the ⚠ + "locks after 1st run" tooltip inline.

### 4.3 Apply suggested defaults

Keep the one-click `seed-defaults/{step}` endpoint we already shipped. Surface a **secondary** "Apply defaults" link beside `Complete Now` on each eligible row:

```
○ 4. Departments & Designations    Apply defaults  |  Complete Now ►
```

Click → backend creates the standard set → checklist re-renders with row ticked. Fast path for users in a hurry.

### 4.4 Once setup is complete

Card disappears (or collapses into a small "Setup complete — view checklist" link in the dashboard footer). Existing KPI tiles + quick links become the primary dashboard surface (which we already shipped).

### 4.5 Sidebar nav + per-route safety net

**Drop the lock icons** on sidebar items — every NavLink reachable. The protective layer is split:

1. **Pay Runs list page** (`/pay-runs`) — still queries `GET /api/v1/payroll-runs/preflight`. Renders empty-state with the blocker list when `ready=false`; Process Payroll button disabled with the same blocker tooltip. Already shipped.
2. **Pay Run detail / FnF routes** (`/pay-runs/:id`, `/pay-runs/:id/fnf`) — these are deep links, can be bookmarked or shared. **Keep a lightweight route-level guard** that redirects to `/pay-runs` (the list page, which surfaces preflight) when the requested run id does not exist in this tenant. Implementation: 404 from `GET /api/v1/payroll-runs/{id}` already does this server-side; the FE just needs to handle the 404 with a `<Navigate to="/pay-runs" replace />` instead of a hard error toast. Specific HTTP-status handling:
   - **401** (token expired) → existing axios refresh-and-retry interceptor (already shipped).
   - **404** (run doesn't exist OR exists with `Status=Deleted` — repo filters Deleted out) → redirect to `/pay-runs`.
   - **403 not possible** today — schema-per-tenant + JWT tenant_id binding routes a cross-tenant id to 404, not 403. If a future feature adds SuperAdmin impersonation, a "no access to this run" page is needed; out of scope here.
3. **Initiate endpoints** (`POST /api/v1/payroll-runs/initiate`, `POST /api/v1/employees/:id/initiate-exit`) — server-side enforcement already covers the prerequisites and returns structured errors (Phase A `InitiatePayrollRunCommand` throws clear DomainException strings; same for InitiateExit). The button-level disable is convenience, not the safety net.
4. **People list page** (`/employees`) — always reachable. The **"Add Employee"** CTA is gated using **`useOnboardingStatus().navGates.people`** (NOT preflight — different concern). Disabled with tooltip listing the specific missing prerequisites (departments, designations, work-locations, salary-structure) when `people.enabled=false`.
5. **Employee detail page** (`/employees/:id`) — reachable; nothing to gate. The exit-initiate sub-action calls the server-side guard (`OrgProfile.DeductorEmployeeId` rule).

Net result: no hard route-level redirects on tenant pages, every dangerous action is guarded by the appropriate signal source, deep links resolve sensibly.

**Signal-source rule (write this on a sticky note for the team):**

| Action | Gate signal |
|---|---|
| Render People list | always allowed |
| Add Employee button | `onboarding-status.navGates.people` |
| Render Pay Runs list / detail | always allowed |
| Process Payroll button | `payroll-runs/preflight` |
| Initiate Exit kebab | `OrgProfile.DeductorEmployeeId` (server-side) |

### 4.6 First-time welcome moment

- Show `Welcome to <CompanyName>` only when `setupComplete === false` AND `welcomeDismissed !== true` (localStorage).
- One-line subtitle + a small "X" to dismiss.
- No confetti animation (we're a B2B serious-payroll product; Zoho can afford fluff because they're huge).

## 5. What we drop / migrate from the current implementation

- `/onboarding` and `/onboarding/:stepId` routes — **deprecate with backward-compat redirects** (see below), then drop in the cleanup phase.
- `OnboardingWizardPage.tsx` — **delete** in Phase 4 (after the redirect window).
- `OnboardingAwareRedirect` + `RequireNavGate` in `router.tsx` — **delete** the redirect + nav gate, **keep** `RequireTenantUser` (the SuperAdmin guard from `418e261` is still useful).
- `AppLayout` lock icons on People + Pay Runs nav items — **delete** the disabled state, restore plain NavLinks.
- `useOnboardingStatus` hook — **keep**, now consumed by `SetupChecklistCard` + the Add Employee button gate.
- `usePayrollRunPreflight` hook — **keep**, still used by Pay Runs page.
- `POST /api/v1/onboarding/seed-defaults/{step}` — **keep**, surfaced as the "Apply defaults" link on each row.
- `GET /api/v1/onboarding/status` — **keep**, only the consumer changes (and gains the `subSteps` field, see §7).
- `SettingsLayout` `?return=onboarding&step=…` banner → rename query value to `?return=dashboard`, copy stays.
- Pay Run detail page 404 handling — **add** a `<Navigate to="/pay-runs" replace />` for the case where `GET /api/v1/payroll-runs/{id}` returns 404 (covers the deep-link safety net once the route-level gate is gone).

### 5.1 `/onboarding` URL deprecation

Existing internal docs, browser bookmarks, and "Set up your organisation" emails point at `/onboarding` and `/onboarding/:stepId`. Hard removal would dead-link them. Two-phase approach:

- **Phase 1 (this redesign):** the route handler **stops rendering `OnboardingWizardPage`** and instead renders a tiny redirect component:
  ```tsx
  function OnboardingDeprecated(): ReactElement {
    // No dedicated telemetry endpoint — usage signal comes from API access logs
    // (the wizard's queries hit /api/v1/onboarding/status, so the dashboard's
    // query is indistinguishable; instead we read GET /api/v1/me + Referer chains
    // from Serilog and from nginx access logs to confirm zero deep-link hits to
    // /onboarding before Phase 4 deletes the route).
    useEffect(() => {
      console.warn('[deprecated-route] /onboarding redirect → /dashboard')
    }, [])
    return <Navigate to="/dashboard" replace />
  }
  ```
  The route itself stays registered for one release so bookmarks/links still resolve. `stepId` is ignored (the new dashboard checklist owns step state).
- **Phase 4 (cleanup):** confirm zero hits via nginx + Serilog access logs over ~2 weeks → delete the route + `OnboardingWizardPage.tsx`.

## 6. New components (frontend)

- `web/src/pages/dashboard/components/SetupChecklistCard.tsx` — the headline card.
- `web/src/pages/dashboard/components/SetupStepRow.tsx` — one row with primary action button + secondary "Apply defaults" + sub-step list.
- `web/src/pages/dashboard/components/WelcomeBanner.tsx` — dismissible welcome line.
- `web/src/pages/dashboard/components/HelpResourcesCard.tsx` — Walkthrough / Help / Email / Phone links.
- Existing `DashboardPage.tsx` — restructure to host the above.

## 7. Backend changes

Minimal:

- **Sub-step breakdown:** the `GET /api/v1/onboarding/status` step DTO gains a `subSteps?: { id, label, complete, hint? }[]` field so the Statutory row can render EPF / ESI / PT / LWF children. Deterministic rules per sub-step:
  - **EPF** — `complete = StatutoryOrgConfig.EpfEnabled = false` (explicit opt-out) OR (`EpfEnabled = true` AND `EpfEstablishmentCode` non-empty). Hint when incomplete: "EPF is enabled but the establishment code is missing".
  - **ESI** — same shape: `complete = !EsiEnabled` OR (`EsiEnabled` AND `EsiEstablishmentCode` non-empty). Hint: "ESI is enabled but the establishment code is missing".
  - **Professional Tax** — `complete = every active WorkLocation.State` either (a) has no PT slabs defined in the seeded slab table for that state (no PT applies → auto-complete) OR (b) has a `PtRegistration` row for that state. Per-state hint when incomplete: "Add PT registration for <state>".
  - **Labour Welfare Fund** — `complete = every active WorkLocation.State` either (a) has no LWF config in the seeded LWF config table (LWF does not apply) OR (b) has an `LwfConfig` row for that state. Per-state hint when incomplete: "Configure LWF for <state>".
  - **Statutory Bonus** — `complete = StatutoryOrgConfig.StatutoryBonusEnabled` is decided (either true or explicitly opted out — current seed defaults to `true`, so this is trivially complete after Phase A backfill).
- **First-name on user / welcome banner:** **scope-limited.** Phase 1 ships with the existing email-prefix fallback only — derives a display name from `user.email.split('@')[0]` and title-cases it. The optional `firstName` enhancement (surface a real first name from `AspNetUsers.first_name` or an `ApplicationUser` extension) is **deferred to a future ticket**, NOT a Phase 2 dependency. Keeps the welcome banner from blocking the redesign on auth-pipeline work.

## 8. Phasing

- **Phase 1 (UX cut-over, ~1 day):**
  - Drop `OnboardingAwareRedirect`, `RequireNavGate`, and the AppLayout lock icons.
  - Replace `OnboardingWizardPage` content with `OnboardingDeprecated` redirect-only component (the route stays registered, see §5.1).
  - Add `SetupChecklistCard` + `SetupStepRow` to the dashboard, consuming the existing `useOnboardingStatus`.
  - Step rows link to Settings using the existing return-banner pattern (rename query value to `?return=dashboard`).
  - Wire the **`Add Employee` button** on `EmployeesPage` to read `onboarding-status.navGates.people` and disable + tooltip when prerequisites are missing.
  - Add the Pay Run detail 404 → `/pay-runs` redirect (covers the deep-link safety net).
- **Phase 2 (sub-steps + welcome, ~0.5 day):**
  - Extend the status DTO with `subSteps` per §7 (EPF/ESI/PT/LWF rules).
  - Render nested list under the Statutory row in `SetupStepRow`.
  - Add `WelcomeBanner` with localStorage dismissal; use email-prefix fallback for the name (no auth-pipeline change in this phase).
- **Phase 3 (help + polish, ~0.5 day):** `HelpResourcesCard`. Tighten copy. Empty-state messaging on KPI tiles.
- **Phase 4 (clean-up, ~0.5 day after a release window of telemetry):**
  - Confirm `/api/v1/telemetry/deprecated-route` shows ~0 hits.
  - Delete `OnboardingWizardPage.tsx`, the `/onboarding` + `/onboarding/:stepId` route registrations, the wizard step rail components.
  - Update `docs/plans/2026-05-24-013-feat-onboarding-wizard-plan.md` with a "SUPERSEDED BY 014" header.

Total ~2.5 dev-days end to end (Phase 4's release-window wait does not count against dev time).

## 9. Risks

- **Phase 1 changes the user's first impression.** If the dashboard isn't ready (empty KPI tiles look bad), the perception cost could outweigh the friction we remove. Mitigation: render greyed-out "Setup needed" placeholders inside each KPI tile until setup is complete.
- **Removing the nav gates means a user *can* navigate to Pay Runs before setup is done.** Two-tier safety: Pay Runs list page reads preflight and renders the blocker list inline; Pay Run detail/FnF deep-link routes fall back to `/pay-runs` when the run id doesn't exist in the tenant (server already returns 404). Initiate endpoints have server-side guards independent of the UI. Verify copy reads well from a brand-new tenant's POV.
- **`Add Employee` button on People page** must read **`onboarding-status.navGates.people`** (NOT preflight). Disabled with tooltip listing the specific missing prerequisites. Small fix in `EmployeesPage`.
- **Statutory sub-step rules are state-dependent.** Per-state PT/LWF derivation needs the seeded slab tables to be the source of truth for "does this state have PT/LWF". Implementation must avoid hardcoding state lists in C# — read from `professional_tax_slabs` and `lwf_state_configs` tables. **Test matrix** (must ship with Phase 2):
  - Tenant with 1 work location in Kerala (PT applies, LWF applies): PT incomplete until registration row exists; LWF incomplete until config row exists.
  - Tenant with 1 work location in Karnataka (PT applies, LWF applies): same as above per Karnataka.
  - Tenant with 2 work locations Kerala + Maharashtra: both states need registrations; sub-step complete only when both are present (mixed-state false-positive guard).
  - Tenant with 1 work location in a state that has no PT slabs in the seed table: PT auto-complete (nothing to configure).
  - Tenant with 1 work location in a state that has no LWF config in the seed table: LWF auto-complete.
  - `StatutoryOrgConfig.EpfEnabled = true` + `EpfEstablishmentCode = null` → EPF sub-step incomplete with hint.
  - Same with `EpfEstablishmentCode = "MH/MUM/0000123/000"` → EPF sub-step complete.
  - `EpfEnabled = false` → EPF sub-step complete (explicit opt-out).
  - Same matrix for ESI.

## 10. Out of scope

- Full Zoho parity: walkthrough video card, webinar registration, "Additional notable features" promo tiles. We don't have these assets yet.
- Multi-language onboarding copy.
- Per-role onboarding (HR vs Finance vs Admin) — single OrgAdmin flow only.

## 11. Open questions

- **OQ-1:** Do we want a "Skip setup" button anywhere? Zoho doesn't have one — the checklist just sits on the dashboard indefinitely. Recommendation: match Zoho (no skip; checklist auto-collapses when complete).
- **OQ-2:** Sub-steps for Salary Structure (e.g., "Template defined", "Components assigned") — worth adding or noisy? Recommendation: skip; salary structure is binary enough.
- **OQ-3:** Should `Apply defaults` link have a confirm prompt? Backend is idempotent so re-clicking is safe. Recommendation: no confirm — keep the fast path fast.

---

Next action: branch off master + start Phase 1.
