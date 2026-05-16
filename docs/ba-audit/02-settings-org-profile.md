# BA Audit Report — Session 3: Zoho Payroll Reference — Settings > Organisation Profile
**Date:** 2026-05-15
**Auditor:** BA Agent
**App URL:** https://payroll.zoho.in/app#/settings/orgprofile
**Session Duration:** ~30 minutes
**Pages Covered:** Settings > Organisation Profile (org profile form, filing address modal, full sidebar navigation inventory)

---

## Executive Summary

This session audits the Organisation Profile settings page of Zoho Payroll India — the foundational configuration page every new tenant completes during onboarding. The page captures the organisation's identity (name, logo, industry), display preferences (date format, field separator), primary address (which becomes the default work location address), filing address selection (constrained to existing work locations), and contact/email configuration. A complete inventory of the Settings sidebar navigation was also captured by expanding all section accordions, revealing 33 distinct settings sub-pages across 9 section groups. No fiscal year or statutory registration number fields appear on this page — those are handled under Tax Details and Statutory Components respectively.

---

## Page Documented

### Settings > Organisation Profile

**URL:** `https://payroll.zoho.in/app#/settings/orgprofile`
**Module:** Settings > Organisation Settings > Organisation
**Access Roles:** OrgAdmin (assumed); HR/Payroll managers may have read-only access (not verified)
**Entry Points:**
- Dashboard onboarding checklist — Step 1 "Set up your organisation" links here
- Settings sidebar > Organisation > Profile
- Direct URL navigation

**Page Title (browser tab):** "Organisation Profile | Settings | Zoho Payroll"
**Status bar ARIA message:** "Settings Organisation Profile page loaded." (confirms screen-reader accessibility)

---

### Page Layout

The page renders inside a full-screen Settings shell. The shell has:
- A top bar with "All Settings" back button, breadcrumb showing "All Settings > lerno" (org name), a Search combobox, and a "Close Settings" button
- A left sidebar (collapsible accordion navigation) occupying ~25% width
- A main content area occupying ~75% width

The main content area has:
1. A page header row: heading "Organisation Profile" (h3) + "Organisation ID: 60071806579" displayed top-right
2. Form body: five logical sections rendered as a single scrollable form (no tabs)
3. A sticky footer row at the bottom: Save button + "* indicates mandatory fields" note
4. A system maintenance banner at the very top of the viewport (dismissible)

---

### Data Fields — Exhaustive

#### Section 1: Organisation Logo

| Field | Type | Required | Current Value | Validation / Constraints | Help Text |
|-------|------|----------|---------------|--------------------------|-----------|
| Organisation Logo | File upload | Optional | No logo uploaded (placeholder icon shown) | Accepted: PNG, JPG, JPEG; Max size: 1 MB; Preferred: 240 x 240 px @ 72 DPI | "This logo will be displayed on documents such as Payslip and TDS Worksheet." |

**Upload mechanism:** "Choose File" button opens OS file picker. A placeholder upload icon and "Upload Logo" label are shown when no logo is set. No drag-and-drop observed.

**Business Rule:** Logo appears on payslips and TDS Worksheet (Form 16 / TDS certificates). Dimensions/DPI constraints are guidance, not hard enforcement (unclear if server-side resize occurs).

**Open Question:** Does Zoho server-side resize the image to 240x240, or does it display at original dimensions? Is there a cropping UI?

---

#### Section 2: Organisation Identity

| Field | Type | Required | Current Value | Validation / Constraints | Help Text |
|-------|------|----------|---------------|--------------------------|-----------|
| Organisation Name | Text input | Yes (*) | `lerno` | Non-empty; free text | "This is your registered business name which will appear in all the forms and payslips." |
| Business Location | Text input | Yes (*) | `India` | Read-only / disabled — cannot be changed after org creation | None visible |
| Industry | Searchable dropdown | Yes (*) | `Technology` | Must select from fixed list (30 options); has search box | None visible |

**Business Location note:** The field is rendered as a disabled text input. This is a critical design decision — once an organisation is created as an Indian payroll entity, the country cannot be changed. This aligns with Zoho Payroll's India-specific product positioning.

**Industry dropdown — all 30 options (alphabetical):**
1. Agency or Sales House
2. Agriculture
3. Art and Design
4. Automotive
5. Construction
6. Consulting
7. Consumer Packaged Goods
8. Education
9. Engineering
10. Entertainment
11. Financial Services
12. Food Services (Restaurants/Fast Food)
13. Gaming
14. Government
15. Health Care
16. Interior Design
17. Internal
18. Legal
19. Manufacturing
20. Marketing
21. Mining and Logistics
22. Non-Profit
23. Publishing and Web Media
24. Real Estate
25. Retail (E-Commerce and Offline)
26. Services
27. Technology
28. Telecommunications
29. Travel/Hospitality
30. Web Designing
31. Web Development
32. Writers

**Industry dropdown UX:** Includes an inline search/filter box (magnifier icon + text input). Selected option shows a checkmark. Currently selected: "Technology".

**Open Question:** Is the Industry field used for any statutory logic (e.g., ESI applicability threshold differs by industry)? Or is it purely informational/reporting?

---

#### Section 3: Display Preferences

| Field | Type | Required | Current Value | Validation / Constraints | Help Text |
|-------|------|----------|---------------|--------------------------|-----------|
| Date Format | Searchable dropdown | Yes (*) | `dd/MM/yyyy [ 15/05/2026 ]` | Must select from fixed list (15 options) | None visible |
| Field Separator | Dropdown | No | `/` | Must select from 3 options: `.`, `-`, `/` | None visible |

**Date Format — all 15 options** (shown with live date preview as of 2026-05-15):

| Format Token | Example |
|---|---|
| MM/dd/yy | 05/15/26 |
| dd/MM/yy | 15/05/26 |
| yy/MM/dd | 26/05/15 |
| MM/dd/yyyy | 05/15/2026 |
| dd/MM/yyyy | 15/05/2026 (**current**) |
| yyyy/MM/dd | 2026/05/15 |
| dd MMM yyyy | 15 May 2026 |
| dd MMMM yyyy | 15 May 2026 |
| MMMM dd, yyyy | May 15, 2026 |
| EEE, MMMM dd, yyyy | Fri, May 15, 2026 |
| EEEEEE, MMMM dd, yyyy | Friday, May 15, 2026 |
| MMM dd, yyyy | May 15, 2026 |
| yyyy MM dd | 2026 05 15 |
| yyyy年MM月dd日 | 2026年05月15日 |
| dd/MMM/yyyy | 15/May/2026 |

**Notable:** The Japanese format `yyyy年MM月dd日` appears despite this being India-only payroll software. This is likely inherited from Zoho's global platform date format library rather than being purposefully included for Indian users. The format tokens follow Java/Unicode CLDR conventions (`EEE`, `MMMM`, `dd`).

**Field Separator — all 3 options:** `.` (period), `-` (hyphen), `/` (forward slash — current).

**Business Rule:** The Field Separator applies to the Date Format — it governs what character separates date components in token-based formats. Example: `dd/MM/yyyy` with separator `/` = `15/05/2026`; with separator `-` = `15-05-2026`. This setting propagates across all documents — payslips, reports, statutory forms.

**Open Question:** Does the Field Separator apply only to date fields, or also to other formatted values (e.g., PAN, phone numbers)?

---

#### Section 4: Organisation Address

**Section label:** "Organisation Address*"
**Section help text:** "This will be considered as the address of your primary work location."

| Field | Type | Required | Current Value | Validation / Constraints | Help Text |
|-------|------|----------|---------------|--------------------------|-----------|
| Address Line 1 | Text input | Yes (implied by *) | `lerno` | Free text | None |
| Address Line 2 | Text input | No | `kazhakoottam` | Free text | None |
| State | Searchable dropdown | Yes (implied) | `Kerala` | Must select from 37 Indian states/UTs | None |
| City | Text input | Yes (implied) | `thiruvananthapuram` | Free text | None |
| PIN Code | Text input | Yes (implied) | `695010` | Numeric, 6 digits (Indian postal code format) | None |

**State dropdown — all 37 Indian states and UTs:**
Andaman and Nicobar Islands, Andhra Pradesh, Arunachal Pradesh, Assam, Bihar, Chandigarh, Chhattisgarh, Dadra and Nagar Haveli and Daman and Diu, Daman and Diu, Delhi, Goa, Gujarat, Haryana, Himachal Pradesh, Jammu and Kashmir, Jharkhand, Karnataka, Kerala, Ladakh, Lakshadweep, Madhya Pradesh, Maharashtra, Manipur, Meghalaya, Mizoram, Nagaland, Odisha, Puducherry, Punjab, Rajasthan, Sikkim, Tamil Nadu, Telangana, Tripura, Uttar Pradesh, Uttarakhand, West Bengal.

**Note on "Daman and Diu":** Both the merged UT "Dadra and Nagar Haveli and Daman and Diu" AND the legacy "Daman and Diu" appear separately. This may cause data quality issues — the legacy UT was merged in 2020. This is a potential compliance concern for PT slab selection (PT is state-specific).

**Business Rule — Critical:** The Organisation Address entered here automatically becomes the address of the **primary work location** named "Head Office". This means the first entry in Work Locations is auto-created from this form. Subsequent work locations must be added via Settings > Work Locations.

**Open Question:** Is Address Line 1 or Address Line 2 the "street address" vs "area/locality"? Indian addresses typically have: Door/Building, Street, Locality, City, District, State, PIN. The two-line model may be insufficient for complex Indian addresses.

---

#### Section 5: Filing Address

**Section label:** "Filing Address"
**Section help text:** "This registered address will be used across all Forms and Payslips."

| Element | Type | Value | Behaviour |
|---------|------|-------|-----------|
| Head Office (label) | Static display text | "Head Office" | Shows current filing address work location name |
| Address display | Static multi-line text | lerno / kazhakoottam / thiruvananthapuram, Kerala 695010 | Read-only; reflects selected work location address |
| Change (link) | Navigation link | `#/settings/orgprofile?change_filing_address=true` | Opens "Update Filing Address" modal |

**Filing Address modal — "Update Filing Address":**
- **Trigger:** Clicking "Change" link
- **URL change:** Appends `?change_filing_address=true` query param (deep-linkable)
- **Modal title:** "Update Filing Address"
- **Field:** "Select Filing Address*" — searchable dropdown constrained to active work locations only
- **Current options:** Only "Head Office" (since only one work location exists)
- **Note displayed in modal:** "Your filing address can only be one of your work locations. To set a new address as your filing address, add that address as a work location in Settings > Work Locations."
- **API call:** `GET /api/v1/worklocations?filter_by=Status.Active` (observed in network tab) — confirms dropdown is populated dynamically from active work locations
- **Buttons:** Save, Cancel, close (X)
- **Escape key:** Closes the modal (triggers `TransitionAborted` error in console — Ember.js routing artefact, non-breaking)

**Business Rule — Critical:** Filing Address is decoupled from Organisation Address. The org can have a registered office at one address (for statutory filing — Form 16, payslips) while the primary work location address differs. This is correct for companies with a registered office different from their operational office.

**Business Rule:** Filing Address can only be a **work location** — it cannot be a free-text address. New addresses must first be added as a Work Location, then selected as the filing address.

---

#### Section 6: Contact Information

**Section label:** "Contact Information"

| Element | Type | Value | Behaviour |
|---------|------|-------|-----------|
| Primary Contact Email Address (label) | Section heading | — | — |
| Help text | Static | "This email address receives reminders and email notifications from Zoho Payroll." | — |
| Contact display | Read-only card | Avatar icon + Name: "abhijithss2255" + Email: "abhijithss2255@gmail.com" | Not editable on this page |
| Emails Are Sent Through (label) | Section heading | — | — |
| Help text | Static | "You can configure the email addresses that would be used in the sender address field for emails sent via Zoho Payroll." | — |
| Sender email display | Read-only card | Icon + "Email address of" + "message-service@mail.zohopayroll.in" | Not editable on this page |
| Warning banner | Alert | "Your primary contact's email address belongs to a public domain. So, emails will be sent from message-service@mail.zohopayroll.in to prevent them from landing in the Spam folder. If you still want to send emails using the public domain, Change Setting" | "Change Setting" is a clickable link |
| Configure Sender Email Preferences | Navigation link | `#/settings/email-preference` | Navigates to email preference settings page |

**Business Rule:** When the primary contact email is a public domain (gmail.com, yahoo.com, etc.), Zoho automatically overrides the sender address to `message-service@mail.zohopayroll.in` to improve deliverability (anti-spam). Custom domain email addresses can override this.

**Open Question:** What is the "Change Setting" link under the warning banner pointing to? (href="#" — JavaScript-driven action, destination not clear from DOM inspection.)

**Open Question:** Is the Primary Contact Email editable here or only via Zoho Accounts profile? There is no edit button/pencil visible on this page for the contact email.

**Network observation:** `GET` request to `https://contacts.zoho.in/file?t=user&ID=60071807338&fs=thumb&nps=400` returns **400 error** — the user avatar thumbnail fails to load. Non-critical UX issue.

---

### Actions

| Action | Element | Trigger | Pre-conditions | Post-behaviour |
|--------|---------|---------|----------------|----------------|
| Save | Button ("Save") | Click | All mandatory fields valid | Saves form; success toast notification expected (not observed in this session) |
| Choose File | Button | Click | None | Opens OS file picker for logo upload |
| Change (Filing Address) | Link | Click | None | Opens "Update Filing Address" modal; URL changes to `?change_filing_address=true` |
| Change Setting (email) | Link (href="#") | Click | None | Unknown — JS-driven; likely opens sender email preference inline |
| Configure Sender Email Preferences | Link | Click | None | Navigates to `#/settings/email-preference` |
| Close Settings | Button | Click | None | Exits settings shell, returns to last active module |

**Save button location:** Fixed at bottom of the scrollable form, above the "* indicates mandatory fields" note. Only one Save button for the entire page (not per-section).

**No Cancel/Reset button** is present on the main form. Changes persist only on Save. Navigating away without saving — behaviour not tested (browser confirm dialog likely).

---

### Business Rules Summary

1. **Business Location is immutable** after org creation — rendered as a disabled field. The country (India) cannot be changed.
2. **Organisation Address = Primary Work Location address** — the two are linked. Changing the org address here updates the "Head Office" work location.
3. **Filing Address is constrained to Work Locations** — it is not a free-text field. New addresses require creation via Work Locations first.
4. **Logo appears on payslips and TDS Worksheets** — logo quality constraints (240x240, 72 DPI, 1 MB) are important for payslip PDF rendering.
5. **Date Format is org-wide** — applies to all payslips, reports, and statutory forms. The default `dd/MM/yyyy` is the Indian standard.
6. **Field Separator applies to date formatting** — 3 options: `.`, `-`, `/`. Currently `/`.
7. **Public domain email triggers sender override** — Zoho forces `message-service@mail.zohopayroll.in` when primary contact uses gmail.com etc.
8. **Industry is informational** — 30 fixed options; no observed statutory logic dependency (unconfirmed).

---

### Data Relationships

| Entity | Relationship | Notes |
|--------|-------------|-------|
| Organisation Profile → Work Locations | One-to-many; Org Address creates "Head Office" work location automatically | Work Locations are the source of truth for PT slab determination |
| Organisation Profile → Filing Address | One-to-one; Filing Address must be one of the active Work Locations | Used on all statutory forms and payslips |
| Organisation Profile → Payslips | Logo, Org Name, Filing Address, Date Format propagate to all payslip PDFs | Date Format is org-wide |
| Organisation Profile → TDS Worksheet | Logo and Filing Address appear on TDS Worksheet (Form 16 equivalent) | |
| Organisation Profile → Primary Contact | Read-only link to Zoho Accounts user record | Not editable on this page |
| Work Locations → API | `GET /api/v1/worklocations?filter_by=Status.Active` — live lookup for Filing Address modal | Only active work locations are selectable |

---

### State & Status Management

No explicit state machine on this page. The form is always editable (no "locked" state observed). There is no concept of a "published" or "finalized" org profile.

**Observed states:**
- **Default / Editable:** Form is always in editable state
- **Logo absent:** Placeholder upload icon shown
- **Logo present:** (not observed — no logo uploaded)
- **Filing Address modal open:** URL has `?change_filing_address=true`; modal overlay displayed
- **Filing Address modal closed:** URL reverts to base route

---

### Error & Edge Cases Observed

| Issue | Severity | Detail |
|-------|----------|--------|
| Avatar thumbnail 400 error | Low | `GET contacts.zoho.in/file?t=user&ID=...` returns 400 — contact avatar fails to load. Non-blocking. |
| TransitionAborted console error | Low | Ember.js router artefact when Escape closes the Filing Address modal mid-transition. Non-blocking. |
| CSP violation (checkhq.com) | Info | `script-src` CSP blocks `cdn.checkhq.com/component-initialize.js`. Report-only mode — no functional impact. Indicates Zoho uses CheckHQ for salary benchmarking but CSP is not yet aligned. |
| Duplicate "Daman and Diu" in State list | Medium | Both legacy "Daman and Diu" and merged "Dadra and Nagar Haveli and Daman and Diu" appear. Relevant for PT slab selection — could cause incorrect PT calculation if wrong entry is chosen. |
| Japanese date format in India-only product | Low | `yyyy年MM月dd日` is not relevant for Indian payroll. Likely inherited from global Zoho platform — cosmetic noise. |
| No Cancel button on main form | Low | If user makes accidental changes and navigates away, there is no easy undo. Browser's back/navigate-away warning is the only safety net. |
| "Change Setting" link (email) href="#" | Medium | The anchor href is "#" which is non-descriptive. The actual action is JavaScript-driven. Deep-linking or accessibility via keyboard may be affected. |

---

### Navigation Paths

**Into this page:**
- Dashboard onboarding checklist > Step 1 "Set up your organisation"
- Settings sidebar > Organisation > Profile
- Direct URL: `#/settings/orgprofile`

**Out of this page:**
- Settings sidebar links (any sidebar item)
- "Configure Sender Email Preferences" → `#/settings/email-preference`
- "Close Settings" → exits settings shell
- Filing Address "Change" → modal (same URL + query param)
- Work Locations note in modal → `#/settings/work-locations` (implied, not a direct link)

---

## Settings Sidebar — Complete Navigation Inventory

The Settings sidebar has three top-level groups and nine accordion sections. All sub-items were captured by expanding all sections via automation.

### Group 1: Organisation Settings

#### Organisation (expanded by default on this page)
| Sub-item | Route |
|---|---|
| Profile | `#/settings/orgprofile` |
| Branding | `#/settings/branding` |
| Work Locations | `#/settings/work-locations` |
| Departments | `#/settings/departments` |
| Designations | `#/settings/designations` |
| Subscriptions | `#/settings/subscription-details` |

#### Users and Roles
| Sub-item | Route |
|---|---|
| Users | `#/settings/users-roles/users` |
| Roles | `#/settings/users-roles/roles` |

#### Taxes
| Sub-item | Route |
|---|---|
| Tax Details | `#/settings/taxes` |

#### Setup & Configurations
| Sub-item | Route |
|---|---|
| Pay Schedule | `#/settings/pay-schedules` |
| Statutory Components | `#/settings/statutory-details/list` |
| Salary Components | `#/settings/salary-components/earnings` |
| Employee Portal | `#/settings/portal/preferences` |
| Claims and Declarations | `#/settings/preferences/fbp` |
| Email Templates | `#/settings/email-templates` |
| Sender Email Preferences | `#/settings/email-preference` |

#### Customisations
| Sub-item | Route |
|---|---|
| Salary Templates | `#/settings/salary-templates` |
| PDF Templates | `#/settings/templates/regular-payslip` |
| Reporting Tags | `#/settings/advanced-reportingtags` |

#### Automations
| Sub-item | Route |
|---|---|
| Workflow Rules | `#/settings/automation/workflows` |
| Actions | `#/settings/automation/actions/alerts` |
| Schedules | `#/settings/automation/schedules` |
| Workflow Logs | `#/settings/automation/logs/alerts` |

---

### Group 2: Module Settings

#### General
| Sub-item | Route |
|---|---|
| Employees & Contractors | `#/settings/employee/contractor` |
| Pay Runs | `#/settings/payrun/custom-approval/list` |
| Salary Revisions | `#/settings/salary-revision/custom-approval/list` |
| Leave & Attendance | `#/settings/holiday-leave/enable-module` |
| Loans | `#/settings/loan/custom-field/list` |

#### Payments
| Sub-item | Route |
|---|---|
| Direct Deposits | `#/settings/direct-deposit` |

---

### Group 3: Extensions & Developer Data

#### Integrations
| Sub-item | Route |
|---|---|
| Zoho Apps | `#/settings/integrations/zoho` |

#### Developer Data
| Sub-item | Route |
|---|---|
| Connections | `#/settings/developer-space/connections` |
| Incoming Webhooks | `#/settings/developer-space/incomingwebhooks` |
| Data Backup | `#/settings/data-backup` |

**Total sidebar items:** 33 distinct settings sub-pages across 9 sections in 3 groups.

**Sidebar search:** A combobox "Search" at the top of the sidebar allows searching across all settings items. Not tested in this session.

---

## Observations & Flags

### Critical Gaps / Compliance Concerns

**[CONCERN] Duplicate "Daman and Diu" State Entry**
The State dropdown in Organisation Address contains both "Daman and Diu" (legacy, merged in 2020) and "Dadra and Nagar Haveli and Daman and Diu" (current merged UT). For our implementation, the State list must contain only the 36 current states/UTs. Using the wrong entry in address or PT configuration could result in incorrect PT slab application.

**[DESIGN NOTE] No Fiscal Year or Statutory Registration Fields Here**
The Organisation Profile page contains NO fields for:
- PAN of the organisation
- TAN (Tax Deduction Account Number)
- CIN / LLPIN (company registration)
- EPFO Registration Number
- ESIC Code
- PT Registration Number
- LWF Registration Number
- Fiscal Year start month

These are critical statutory fields. Zoho places them under "Tax Details" (`#/settings/taxes`) and "Statutory Components" (`#/settings/statutory-details/list`). Our implementation must ensure these are not mixed into the Org Profile page but are clearly separated under a Statutory / Tax configuration section.

**[DESIGN NOTE] Industry Field — Statutory Relevance Unknown**
Industry type can affect ESI applicability (certain industries may have different thresholds or exemptions under ESIC Act). Must clarify with Zoho whether Industry drives any statutory logic or is purely for reporting.

### Ambiguities

- [ ] Is the logo image server-side cropped/resized to 240x240 on upload, or stored at original dimensions?
- [ ] Does "Field Separator" apply only to dates, or to other formatted values?
- [ ] What does "Change Setting" (email warning banner) actually do — is it an inline toggle or does it navigate somewhere?
- [ ] Is the Organisation Name used verbatim on statutory forms (e.g., Form 16 employer name), or does TAN/PAN name take precedence?
- [ ] Why does Business Location show as "India" (not "IN" or a dropdown)? Is this driven by Zoho Accounts locale?
- [ ] Primary Contact Email — is this editable only via Zoho Accounts (not in Zoho Payroll settings)?

### Well-Implemented

- **Live date preview in Date Format dropdown** — each option shows the actual current date formatted that way (e.g., "dd/MM/yyyy [ 15/05/2026 ]"). Excellent UX — eliminates format ambiguity.
- **Filing Address modal constraint** — restricting filing address to work locations is architecturally sound. It prevents orphan addresses and ensures address data consistency.
- **API query for work locations** — `?filter_by=Status.Active` ensures inactive/deleted work locations cannot be selected as filing address.
- **Deep-linkable modal** — filing address modal appends `?change_filing_address=true` to URL, making it bookmarkable and back-button navigable.
- **Public domain email warning** — proactive UX to prevent payroll emails landing in spam for new orgs.
- **ARIA status message** — "Settings Organisation Profile page loaded." confirms screen reader support.
- **State list completeness** — 37 entries cover all current Indian states and UTs (Ladakh and J&K separated post-2019 reorganisation), plus legacy entries.
- **Section help text quality** — each section has a concise, accurate help text that explains what the field does and where it appears.

---

## Implications for Our Implementation

Based on this audit, our Organisation Profile / Settings page should implement:

1. **Logo upload** — file type validation (PNG/JPG/JPEG), size cap (1 MB), dimension guidance. Server-side: store in MinIO, path in org settings table.
2. **Organisation Name** — maps to `organisations.name` in our schema. Must propagate to payslip PDF header and all statutory forms.
3. **Business Location** — store as immutable `country_code` = "IN" set at tenant provisioning time. Display as read-only.
4. **Industry** — store in org settings. Use same 30-option list (inheriting Zoho's taxonomy). Mark as informational for v1; do not wire to any statutory logic until ESI industry-specific rules are needed.
5. **Date Format** — store format token string in org settings. Apply globally to all date rendering in payslips, reports, API responses. Default: `dd/MM/yyyy`.
6. **Field Separator** — store character in org settings. Apply to date format token replacement only (initially). Default: `/`.
7. **Organisation Address** — implement as Address value object: Line1, Line2, City, State (enum from our 36-state list — exclude legacy "Daman and Diu"), PINCode. Link to Work Locations: auto-create "Head Office" work location from this address at setup.
8. **Filing Address** — `organisations.filing_work_location_id` FK → `work_locations.id`. Modal with live lookup of active work locations. Default to "Head Office" at creation.
9. **Contact Information** — display-only on this page. Primary contact email comes from tenant admin user record (seeded at provisioning). Link to email preferences page.
10. **Statutory fields (NOT on this page)** — PAN, TAN, EPFO Reg, ESIC Code, PT Reg, LWF Reg go under separate "Tax Details" / "Statutory Configuration" settings pages.
11. **State list** — use 36 states/UTs (post-2020, excluding "Daman and Diu" legacy entry; include "Dadra and Nagar Haveli and Daman and Diu" as the merged UT).

---

## Open Questions
- [ ] Logo: server-side resize or stored at original dimensions?
- [ ] Industry field: any statutory logic dependency (ESI, etc.) or purely informational?
- [ ] "Change Setting" email link: what action does it trigger?
- [ ] Organisation Name vs TAN-registered name: which appears on Form 16?
- [ ] Primary Contact Email: editable only via Zoho Accounts, not Zoho Payroll settings?
- [ ] Address: should we support District as a separate field (common in Indian addresses for pin-to-district mapping)?

---

## Next Session

**Resume from:** Settings > Taxes (`#/settings/taxes`) — This page captures Tax Details including likely PAN, TAN, and TDS-related org-level configuration. Critical for Form 16 and TDS returns.

**Alternatively:** Settings > Pay Schedules (`#/settings/pay-schedules`) — captures pay frequency config (Monthly, Bi-monthly, etc.) which feeds Payroll Run.

**Alternatively:** Settings > Statutory Components (`#/settings/statutory-details/list`) — captures PF, ESI, PT, LWF toggles and thresholds.

**Recommended order:** Taxes → Statutory Components → Pay Schedules → Salary Components → Work Locations (to understand full statutory setup flow in sequence).

**Pending questions from this session:** See Open Questions above.
