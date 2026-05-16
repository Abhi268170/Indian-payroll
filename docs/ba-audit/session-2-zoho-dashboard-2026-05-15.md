# BA Audit Report — Session 2: Zoho Payroll Dashboard (Reference Product)
**Date:** 2026-05-15
**Auditor:** BA Agent
**App URL:** https://payroll.zoho.in/app#/home/dashboard
**Session Duration:** ~30 minutes
**Pages Covered:** Dashboard (Getting Started / Onboarding)
**Org State:** Fresh org — 2 of 7 onboarding steps completed (Organisation Details done, Prior Payroll marked done; steps 2–6 pending)

---

## Executive Summary

The Zoho Payroll dashboard for a fresh organisation is a pure onboarding orchestration surface. It presents a linear 7-step setup checklist, contextual help resources, support contacts, and a "Find help & resources" panel — all in a single scrollable page. No operational payroll data (summaries, charts, recent runs) is shown until setup is complete. This is an important product decision: the dashboard graduates from an onboarding guide to an operational summary only after all mandatory setup steps are finished.

---

## Page Identity

| Attribute | Value |
|-----------|-------|
| Page name | Dashboard (Getting Started state) |
| URL | `https://payroll.zoho.in/app#/home/dashboard` |
| Route pattern | `#/home/dashboard` |
| Module | Home / Onboarding |
| Page title | `Dashboard \| Zoho Payroll` |
| Access roles | Org Admin (observed); likely also Payroll Manager with restricted view |
| Entry points | Left sidebar "Dashboard" link; default landing after login |

---

## Page Layout & Information Architecture

The page uses a three-zone layout:

```
┌──────────────────────────────────────────────────────────────┐
│  TOP BAND (global header)                                     │
├──────────────┬───────────────────────────────────────────────┤
│              │  Welcome banner + support phone               │
│  LEFT        │  ─────────────────────────────────────────── │
│  SIDEBAR     │  Live webinar promo strip                     │
│  (nav)       │  ─────────────────────────────────────────── │
│              │  Onboarding checklist (7 steps, expandable)   │
│              │  ─────────────────────────────────────────── │
│              │  Find help & resources panel                  │
│              │  ─────────────────────────────────────────── │
│              │  Contact / support footer cards               │
│              │  ─────────────────────────────────────────── │
│              │  Employee portal mobile app promo             │
│              │  ─────────────────────────────────────────── │
│              │  Global footer (other Zoho apps + links)      │
└──────────────┴───────────────────────────────────────────────┘
```

Below the fold: global footer with cross-product links, copyright notice.

---

## Section 1: Global Header (Top Band)

| Element | Type | Detail |
|---------|------|--------|
| Zoho Payroll logo + "Payroll" wordmark | Image + text | Top-left; clicking returns to dashboard |
| Advanced Search button | Icon button | Opens employee search overlay |
| "Search in Employee" | Text input | Inline search; placeholder text is "Search in Employee" |
| Trial expiry notice | Inline text | "Your trial expires in 14 day(s)." — countdown shown |
| Upgrade button | CTA button | Triggers plan upgrade flow |
| Org switcher "lerno" | Dropdown button | Shows org name with chevron; likely switches between orgs |
| "Refer and Earn" | Icon button | Referral program entry point |
| Notifications bell | Icon button | Opens notification panel |
| Settings gear | Icon button | Shortcut to settings (same as sidebar Settings link) |
| User avatar "A" | Circular button | Account menu: profile, logout, etc. |
| (Unnamed icon) | Image/icon | Rightmost; purpose unclear — likely Zoho One or help launcher |

**Business rule noted:** Trial countdown is displayed persistently in the header on every page. "14 day(s)" is the trial period for a fresh org.

---

## Section 2: Maintenance / System Alert Banner

**Banner text (verbatim):**
> "Planned India Data Center Maintenance on 10th May 2026 and 17th May 2026, between 06.30AM to 09.30AM IST."

| Element | Type | Detail |
|---------|------|--------|
| Banner body | Dismissible bar | Appears as a bottom-of-screen overlay (z-layer above footer) |
| "Know more" | External link | Points to `https://blog.zoho.com/general/maintenance%20may%202026.html` |
| "Don't show again" | Dismiss control | Persists dismiss preference (likely via localStorage or user preference API) |

**Note:** This is a transient system notification, not a product feature — but it documents that Zoho Payroll has a mechanism to push global maintenance banners to all tenants.

---

## Section 3: Left Sidebar Navigation

The sidebar is collapsible (Collapse/Expand button at bottom). All items observed:

| # | Nav Item | Route | Icon | Notes |
|---|----------|-------|------|-------|
| 0 | Getting Started | `#/home/dashboard` | Checklist icon | Expandable accordion item; shown as top-level group |
| 1 | Dashboard | `#/home/dashboard` | Dashboard icon | Separate link below Getting Started |
| 2 | Employees | `#/people/employees` | Person icon | Employee master list |
| 3 | Pay Runs | `#/payruns` | Pay icon | Payroll run management |
| 4 | Approvals | — | Approval icon | Expandable sub-menu (accordion); sub-items not visible in collapsed state |
| 5 | Form 16 | `#/taxes-and-forms/form16` | Form icon | Direct link to Form 16 generation |
| 6 | Loans | `#/loans` | Loan icon | Loan management module |
| 7 | Giving | `#/donations` | Gift/donate icon | Charitable giving / donation module |
| 8 | Documents | `#/documents/folder` | Folder icon | Document management |
| 9 | Reports | `#/reports` | Reports icon | All statutory and operational reports |
| 10 | Settings | `#/settings` | Gear icon | App-wide configuration |
| — | Contact Support | `#/support` | Support icon | Bottom of sidebar; always visible |

**Observations:**
- "Getting Started" appears as a collapsible heading with an expand chevron — it likely contains sub-items (the 7 setup steps) when expanded.
- "Approvals" is an accordion with nested items — sub-items were not captured; a follow-up audit of its expanded state is needed.
- There is one unnamed `listitem` in the nav list (between "Giving" and "Documents") — this is likely a separator or a hidden/conditional nav item that warrants investigation.

---

## Section 4: Welcome Banner (Below Header)

| Element | Type | Content |
|---------|------|---------|
| Heading | H3 | "Welcome abhijithss2255!" — personalised with org admin username |
| Subtitle paragraph | Text | "Set up your organisation before you run your first payroll." |
| Separator | Horizontal rule | Visually separates heading from support info |
| Support info paragraph | Text + icon | "Need help? Call us Mon–Fri, 9 AM–7 PM at 18005726671 (toll-free)." |
| "Instant Helper" button | Floating button | Opens an in-app guided help overlay (contextual wizard); icon only |

---

## Section 5: Live Webinar Promo Strip

| Element | Type | Content |
|---------|------|---------|
| Webinar label | Text + icon | "Live webinar: An overview of Zoho Payroll" |
| Schedule label | Text + icon | "Tuesday or Thursday @3:30PM IST" |
| "Register Now" | External link | `https://meet.zoho.in/umux-wdb-zgw` |

**Note:** This is a persistent promotional strip shown to all new orgs on the dashboard. It is not dismissible (no close button observed).

---

## Section 6: Onboarding Checklist ("Get Started with Zoho Payroll")

**Header:**
- H2: "Get started with Zoho Payroll"
- H3: "Complete the following steps to have a hassle-free payroll experience"
- Progress indicator: **"2/7 Completed"** (text badge, top-right of checklist card)

### Step-by-Step Detail

Each step is a collapsible accordion row. Completed steps show a "Completed" link (which navigates to the relevant settings page). Pending steps show a "Complete Now" CTA button. Step 4 is expanded by default (showing sub-items).

| # | Step Label | Status | CTA / Link | Route |
|---|-----------|--------|-----------|-------|
| 1 | Add Organisation Details | **Completed** | "Completed" link | `#/settings/orgprofile` |
| 2 | Provide your Tax Details | Pending | "Complete Now" button | (navigates to tax details settings) |
| 3 | Configure your Pay Schedule | Pending | "Complete Now" button | (navigates to pay schedule settings) |
| 4 | Set up Statutory Components | Pending | No CTA on header — sub-items expanded | — |
| 5 | Set up Salary Components | Pending | "Complete Now" button | (navigates to salary components) |
| 6 | Add Employees | Pending | "Complete Now" button | (navigates to employee creation) |
| 7 | Configure Prior Payroll | **Completed** | "Completed" link | `#/prior-payroll` |

**Step 4 Sub-items (expanded by default — statutory component cards):**

| Sub-item | Route | Notes |
|----------|-------|-------|
| Employees' Provident Fund | `#/settings/statutory-details/list` | Card with EPF logo |
| Employees' State Insurance | `#/settings/statutory-details/list/esi` | Card with ESI logo |
| Labour Welfare Fund | `#/settings/statutory-details/list/lwf` | Card with LWF logo |
| Professional Tax (Configured based on Work Location) | `#/settings/statutory-details/list/pt` | PT note explicitly states it is work-location-based |

**Business rules observed:**
- Step 7 ("Configure Prior Payroll") is marked Completed on a brand-new org — this is either a default/skip behaviour or the test org has this step pre-marked. This warrants clarification: is Prior Payroll configuration optional (can be skipped), or does it auto-complete when there is no prior payroll data?
- Professional Tax is scoped to work location, not to the organisation as a whole. This is architecturally significant: PT configuration must be maintained per-branch/work-location (since PT slabs vary by state).
- The ordering of steps (Org Details → Tax Details → Pay Schedule → Statutory → Salary Components → Employees → Prior Payroll) implies a strict dependency chain: you cannot run payroll without completing these in roughly this sequence.

---

## Section 7: "Additional Notable Features" Panel

Displayed below the 7 steps, inside the checklist card:

| Feature | Route | Description |
|---------|-------|-------------|
| Direct Deposit | `#/settings/direct-deposit` | Bank transfer / NEFT setup for payroll disbursement |
| Salary Templates | `#/settings/salary-templates` | Reusable salary structure templates |
| Auto Reminder for IT & POI Declaration | `#/settings/preferences/it-declaration` | Automated reminders to employees for investment declaration |
| Employee Custom Field | `#/settings/employee/custom-field/list` | Custom metadata fields on employee profiles |

**Note:** These are optional/supplementary features, not part of the mandatory setup flow. They are surfaced proactively on the dashboard to drive feature adoption.

---

## Section 8: "Find Help & Resources" Panel

### Sub-panel A: Learn how to use Zoho Payroll

| Element | Type | Detail |
|---------|------|--------|
| Panel heading | H4 | "Learn how to use Zoho Payroll" |
| "Watch Complete Walkthrough Video" | Button | Opens embedded or linked walkthrough video |

### Sub-panel B: Help & Support

| Link | URL |
|------|-----|
| Help | `https://zoho.in/in/payroll/help/` |
| FAQ | `https://zoho.in/in/payroll/kb/` |
| Forum | `https://help.zoho.com/portal/en/community/zoho-payroll` |

---

## Section 9: Support Contact Cards (Below Help Panel)

**Card 1 — Email support:**
> "Need Clarification? Drop us an email and we'll get back to you"
- "Drop us an email" is an inline link pointing to `#/support`

**Card 2 — Phone support:**
> "You can directly talk to us every Monday to Friday"
> Toll Free: 18005726671 (9:00 AM to 7:00 PM)

---

## Section 10: Employee Portal Mobile App Promo

| Element | Type | Detail |
|---------|------|--------|
| Heading | H4 | "Payroll portal app for your employees; accessible anywhere anytime" |
| App Store link | External link | `https://itunes.apple.com/in/app/employee-portal-zoho-payroll/id1450810850` |
| Play Store link | External link | `https://play.google.com/store/apps/details?id=com.zoho.payroll&referrer=utm_source%3Dweb_app_admin%26utm_medium%3Dfooter&gl=IN` |

**Note:** UTM parameters on the Play Store link (`utm_source=web_app_admin`, `utm_medium=footer`) confirm this is a tracked referral link — Zoho measures app install conversions from the web admin dashboard.

---

## Section 11: Global Footer

### Column 1 — Other Zoho Apps
| Link | URL |
|------|-----|
| Accounting Software | `https://zoho.com/books/` |
| HR Software | `https://zoho.com/people/` |
| Invoice Software | `https://zoho.com/invoice/` |
| Inventory Management | `https://zoho.com/inventory/` |
| Billing Software | `https://zoho.com/billing/` |
| Expense Reporting | `https://zoho.com/expense/` |
| CRM & Other Apps | `https://zoho.com/` |

All footer links carry UTM params: `utm_source=payroll&utm_medium=footer&utm_campaign=referral`

### Column 2 — Help & Support
| Link | URL |
|------|-----|
| Contact Support | `#/support` |
| Help Documentation | `https://zoho.in/in/payroll/help/` |
| FAQ | `https://zoho.in/in/payroll/kb/` |
| Training Program | `https://zoho.in/in/payroll/payroll-training/` |

### Column 3 — Quick Links
| Link | URL |
|------|-----|
| Features | `https://zoho.in/in/payroll/employees-onboarding/` |
| Integrations | `https://zoho.in/in/payroll/integrations/` |
| What's New? | `https://zoho.in/in/payroll/whats-new/` |

**Copyright:** © 2026, Zoho Corporation Pvt. Ltd. All Rights Reserved.

---

## Console Errors & Technical Observations

| Severity | Type | Detail |
|----------|------|--------|
| INFO (non-blocking) | CSP violation (report-only) | `https://cdn.checkhq.com/component-initialize.js` blocked by Content-Security-Policy. Policy is report-only — no functional impact but indicates a third-party script (CheckHQ, likely a compliance/payroll integration) is being attempted |
| WARNING | iframe sandbox escape risk | `allow-scripts` + `allow-same-origin` on same iframe in `inproductsdk.js` — standard in-product messaging SDK; known browser warning |
| ASSERT | TransitionAborted | Ember.js router transition abort on page load — likely a redirect from a deeper route to dashboard. Benign but indicates the app is built on Ember.js (vendor bundle name `vendor-a5c189eb3ced617ff41646d9ec8dfad5.js`) |
| ERROR | HTTP 400 | `https://contacts.zoho.in/file?t=user&ID=60071807338&fs=thumb&nps=400` — user avatar/thumbnail fetch failure. Non-critical; avatar placeholder shown instead |

**Architecture signal:** The vendor bundle confirms Zoho Payroll frontend is built on **Ember.js**, not React. The route system is Ember Router (`#/home/dashboard` hash-based routes). This is relevant context for our own React build.

---

## Data Relationships Inferred from Dashboard

```
Organisation (Tenant)
  ├── Onboarding Checklist (1:1 per org)
  │     ├── Step 1: OrgProfile → #/settings/orgprofile
  │     ├── Step 2: TaxDetails → #/settings/tax-details (inferred)
  │     ├── Step 3: PaySchedule → #/settings/pay-schedule (inferred)
  │     ├── Step 4: StatutoryComponents
  │     │     ├── EPF  → #/settings/statutory-details/list
  │     │     ├── ESI  → #/settings/statutory-details/list/esi
  │     │     ├── LWF  → #/settings/statutory-details/list/lwf
  │     │     └── PT   → #/settings/statutory-details/list/pt
  │     ├── Step 5: SalaryComponents → #/settings/salary-components (inferred)
  │     ├── Step 6: Employees → #/people/employees
  │     └── Step 7: PriorPayroll → #/prior-payroll
  └── Settings
        ├── Direct Deposit → #/settings/direct-deposit
        ├── Salary Templates → #/settings/salary-templates
        ├── IT Declaration Reminders → #/settings/preferences/it-declaration
        └── Employee Custom Fields → #/settings/employee/custom-field/list
```

---

## Business Rules Summary

1. **Onboarding gate:** The dashboard in new-org state is entirely an onboarding surface. No payroll data, charts, or run summaries are displayed until setup is complete.
2. **Progress tracking:** Progress is expressed as a fraction ("2/7 Completed") with per-step status (completed / pending). There is no percentage bar — just a text counter.
3. **Step completion is non-linear in practice:** Step 7 (Prior Payroll) is marked Completed while steps 2–6 are pending. This means steps can be completed out of order, OR Prior Payroll can be auto-skipped/defaulted for new orgs with no prior payroll history.
4. **PT is work-location scoped:** Professional Tax configuration is explicitly labelled "(Configured based on Work Location)" — multi-branch orgs in different states each need separate PT configuration.
5. **Trial timer is persistent:** The 14-day trial countdown is visible in the global header on every page — it is a session-persistent nudge, not a one-time banner.
6. **Support is always accessible:** Phone and email support contacts appear in three locations: (1) welcome banner, (2) support contact cards, (3) global footer. The sidebar also has a permanent "Contact Support" link.
7. **Instant Helper:** An "Instant Helper" floating button is available on the dashboard — this is a contextual guided walkthrough. Its content/steps are not captured in this session and warrant a follow-up click audit.

---

## State Machine: Dashboard Onboarding

```
New Org Created
    │
    ▼
Dashboard (Onboarding State) — shows 7-step checklist
    │
    ▼ [All 7 steps completed]
Dashboard (Operational State) — shows payroll summaries, charts, recent runs
    │                          (not yet observed — requires full setup)
    ▼
Subsequent logins → Operational Dashboard (no onboarding checklist)
```

The transition from onboarding state to operational state has not been audited yet — it requires completing all setup steps.

---

## Navigation Paths

**Into this page:**
- Login redirect (default post-login destination)
- Left sidebar "Dashboard" link
- Left sidebar "Getting Started" link (same route)
- Browser back-navigation from any settings page

**Out of this page (direct links observed):**
- `#/settings/orgprofile` (Step 1 — Completed link)
- Tax Details settings (Step 2 — Complete Now)
- Pay Schedule settings (Step 3 — Complete Now)
- `#/settings/statutory-details/list` (Step 4 EPF)
- `#/settings/statutory-details/list/esi` (Step 4 ESI)
- `#/settings/statutory-details/list/lwf` (Step 4 LWF)
- `#/settings/statutory-details/list/pt` (Step 4 PT)
- Salary Components settings (Step 5 — Complete Now)
- `#/people/employees` (Step 6 — Complete Now)
- `#/prior-payroll` (Step 7 — Completed link)
- `#/settings/direct-deposit`
- `#/settings/salary-templates`
- `#/settings/preferences/it-declaration`
- `#/settings/employee/custom-field/list`
- `#/support`
- Multiple external Zoho help/product URLs

---

## Observations & Flags

### Critical Gaps / Open Questions

- [ ] **Step 7 anomaly:** Why is "Configure Prior Payroll" marked Completed on a fresh org that has not configured payroll before? Determine if this step auto-completes (default = no prior payroll), is skippable, or was manually marked on this test org.
- [ ] **"Approvals" sub-menu:** The Approvals nav item is an accordion. Its sub-items were not captured. A dedicated click audit of the Approvals section is needed.
- [ ] **Unnamed nav listitem:** There is an anonymous `listitem` in the sidebar between "Giving" and "Documents". Determine if this is a separator, a hidden feature, or a rendering artefact.
- [ ] **Instant Helper content:** The floating "Instant Helper" button launches a contextual walkthrough. Its steps, triggers, and content are undocumented.
- [ ] **Operational dashboard state:** The dashboard in post-setup state (all 7 steps complete) has not been audited. It likely shows payroll summaries, recent run cards, compliance alerts, and employee count KPIs. This is the reference for our own dashboard design.
- [ ] **"Complete Now" routes:** Steps 2, 3, 5, and 6 show "Complete Now" buttons but their exact destination routes were not captured (buttons trigger navigation rather than being `<a>` tags). A click audit on each is needed.
- [ ] **Step 4 expand/collapse behaviour:** Step 4 appears expanded by default (showing sub-items). Determine if it auto-expands because it is the next pending step, or if it is always expanded.
- [ ] **CheckHQ integration:** The CSP violation for `cdn.checkhq.com` suggests Zoho Payroll may integrate with CheckHQ (US payroll compliance tool — unusual for India). Investigate whether this is a multi-region asset or a mis-included script.

### Ambiguities

- The "lerno" org switcher button — is "lerno" the test org name? Confirm whether clicking it allows switching between multiple orgs (multi-org admin support).
- The rightmost icon in the global header (after user avatar "A") has no accessible label — its function is undocumented.

### Well-Implemented

- The onboarding checklist is an excellent UX pattern: it provides a clear progress indicator (2/7), contextual CTAs per step, and auto-expands the next actionable step (Step 4 with sub-items visible). This gives admins a clear "what next" path.
- Step 4's statutory sub-items are all linked directly with individual routes per statutory type (EPF/ESI/LWF/PT) rather than a single monolithic settings page — this reflects the reality that each statutory component has independent configuration.
- The PT label explicitly calls out "(Configured based on Work Location)" — this is a proactive UX disclosure that prevents admins from expecting a single PT setting for the whole org.
- Support contact information (phone + email) is surfaced three times on this page, reducing friction for new users who need help during setup.
- Trial expiry countdown in the header is non-intrusive but persistent — appropriate pressure without being disruptive.

---

## Screenshots

- `docs/ba-audit/screenshots/01-dashboard-top.png` — viewport screenshot (above fold)
- `docs/ba-audit/screenshots/01-dashboard-full.png` — full-page screenshot (all content)

---

## Next Session

**Resume from:** Click audit of each onboarding step's "Complete Now" destination, starting with Step 2 (Tax Details). Then audit the operational dashboard state (requires completing setup or using a fully configured test org).

**Priority follow-ups:**
1. Step 2: Provide Tax Details — full field audit
2. Step 3: Configure Pay Schedule — full field audit
3. Step 4 statutory sub-pages: EPF, ESI, LWF, PT settings pages
4. Step 5: Salary Components setup
5. Approvals accordion — expand and document sub-items
6. Operational dashboard (post-setup state) — KPIs, charts, payroll run summary cards
