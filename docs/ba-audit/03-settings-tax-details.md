# BA Audit Report — Session 4: Settings > Tax Details
**Date:** 2026-05-15
**Auditor:** BA Agent (Playwright-assisted)
**App URL:** https://payroll.zoho.in/app#/settings/taxes
**Session Duration:** ~25 minutes
**Pages Covered:** Settings > Taxes > Tax Details

---

## Executive Summary

The Tax Details page is the single settings page under the "Taxes" group in Organisation Settings. It captures two logically distinct data groups: Organisation-level TDS credentials (PAN, TAN, AO code, payment frequency) and the Tax Deductor's personal identity (who is responsible for signing TDS returns). The Deductor section implements a non-obvious conditional form — Employee selection (dropdown against Employee Master) vs Non-Employee entry (free-text with additional Designation field). The Save flow is always-enabled but performs pre-submit format validation with a banner error pattern. One important gap: Tax Payment Frequency is hardcoded to "Monthly" and is not configurable from this UI.

---

## Page Identity

| Attribute | Value |
|-----------|-------|
| Page Title | "Tax Details \| Settings \| Zoho Payroll" |
| URL / Route | `https://payroll.zoho.in/app#/settings/taxes` |
| Module | Organisation Settings > Taxes |
| Sidebar Location | Organisation Settings > Taxes (accordion) > Tax Details |
| Parent Accordion | "Taxes" — has only one child link: "Tax Details" |
| Access Roles | Org Admin (assumed — not tested with lower roles in this session) |
| Entry Points | (1) Left sidebar: Organisation Settings > Taxes > Tax Details; (2) Onboarding checklist "Set up taxes" link; (3) Settings search |
| API Endpoints Observed | `GET /api/v1/settings/incometaxdetails` (page load), `PUT /api/v1/settings/incometaxdetails` (save), `GET /api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll&search_text=` (Deductor Name dropdown) |

---

## Layout

The page uses the standard Settings shell: full-width left sidebar with accordion navigation + main content area. The main area has:

- Page heading row: "Tax Details" (h3) + "Instant Helper" button (top right, with icon)
- A single `<form>` element containing two visually separated sections:
  1. "Organisation Tax Details" (h4)
  2. "Tax Deductor Details" (h4)
- A `<hr>` separator before the action row
- Action row: "Save" button (left) + "* indicates mandatory fields" note (right)

No tabs, no modals within this page (aside from the validation banner). No pagination. No table view.

---

## Section 1: Organisation Tax Details

### Data Fields

| Field | Type | Required | Default | Validation | Format / Constraint | Notes |
|-------|------|----------|---------|------------|---------------------|-------|
| PAN | Text input | Yes (marked with `*`) | Empty | Non-empty AND regex match required. Error: "Enter a valid PAN." | Placeholder: `AAAAA0000A`. Help text below field: "Format: AAAAA0000A" | PAN of the organisation (not individual). Used as TAN-linked entity identifier in TDS returns. PAN format per IT Dept: 5 alpha + 4 numeric + 1 alpha (all uppercase). Field appears to enforce a max length — entering "INVALID123" (10 chars) showed only "INVAL123" in post-save snapshot, suggesting possible input masking to 10 chars but alpha-numeric validation is the gate. |
| TAN | Text input | No (no `*`) | Empty | Format validated only on save if filled. Same error banner pattern. | Placeholder: `AAAA00000A`. Help text appears on focus: "Format: AAAA00000A" | Tax Deduction Account Number — 10 chars: 4 alpha + 5 numeric + 1 alpha. Required for TDS challan submission (Form 26Q/24Q). Optionality here is a UX choice — Zoho apparently allows saving without TAN but TAN will be needed before payroll can be finalized. |
| TDS circle / AO code | Segmented text input (4 parts) | No | Empty | Format: each segment has placeholder indicating expected length (AAA / AA / 000 / 00) | 4 separate textboxes separated by `/` dividers: Segment 1 (AAA — 3 alpha), Segment 2 (AA — 2 alpha), Segment 3 (000 — 3 numeric), Segment 4 (00 — 2 numeric). Full code format: `AAAAAAA000000` compressed or `AAA/AA/000/00` expanded | AO = Assessing Officer. Required for TDS return filing (Form 24Q) — identifies the Income Tax circle that governs this deductor. Tooltip (hover on `i` icon): "This number can be obtained from Income tax office or you can login into your Income tax account and navigate to **My Profile** section to find this number." |
| Tax Payment Frequency | Disabled text input (read-only) | N/A | "Monthly" | Not editable | Single static value displayed | This field is NOT a dropdown — it renders as a disabled `<input type="text">` with value "Monthly". The tooltip explains the statutory rule (see Business Rules). No configuration is possible from this UI. The Tax Payment Frequency for TDS is legislatively fixed as Monthly for most deductors (TDS due by 7th of following month, except March where it is 30th April). This appears to be a display-only statutory reference, not a configurable setting. |

### Tooltip Text (Exact)

**TDS circle / AO code tooltip:**
> "This number can be obtained from Income tax office or you can login into your Income tax account and navigate to **My Profile** section to find this number."

**Tax Payment Frequency tooltip:**
> "The Tax Deducted at Source (TDS) for each month should be paid to the Income Tax Department on or before the 7th of the following month. Only for the month of March, TDS should be deposited on or before the 30th of April."

---

## Section 2: Tax Deductor Details

This section renders differently depending on the Deductor's Type radio selection. It implements conditional form rendering (not just conditional enabling).

### Deductor's Type (Radio Group)

| Option | Value | Default |
|--------|-------|---------|
| Employee | `Employee` | Yes (pre-selected) |
| Non-Employee | `Non-Employee` | No |

Switching between options causes an immediate DOM re-render of the fields below.

---

### Sub-form A: When "Employee" is selected (default)

| Field | Type | Required | Default | Behaviour | Notes |
|-------|------|----------|---------|-----------|-------|
| Deductor's Name | Searchable combobox (dropdown) | Not marked with `*` | Placeholder: "Select a Tax Deductor" | Opens a search-enabled dropdown; fires `GET /api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll&search_text=` on open; empty org shows "Sorry! No results found" | Data source: Active employees in Employee Master. The `employee_type=employee` filter excludes other personnel categories. The `module=Payroll` filter narrows to payroll-eligible employees. Search is live-query (search_text param). |
| Deductor's Father's Name | Text input | Not marked with `*` | Empty | **Disabled** when "Employee" is selected. Presumably auto-populated from the selected employee's record once an employee is chosen. | Father's Name is a mandatory field on Form 24Q (TDS return). Auto-population from Employee Master record is the likely design intent. |

### Sub-form B: When "Non-Employee" is selected

| Field | Type | Required | Default | Behaviour | Notes |
|-------|------|----------|---------|-----------|-------|
| Deductor's Name | Free-text input | Not marked with `*` | Empty | **Enabled** — plain text entry, no lookup | For external deductors (e.g., a CA or external HR) who are not in the Employee Master. |
| Deductor's Father's Name | Text input | Not marked with `*` | Empty | **Enabled** — plain text entry | Enabled and editable when Non-Employee is selected, unlike the Employee mode. |
| Deductor's Designation | Text input | Not marked with `*` | Empty | **Only visible in Non-Employee mode** — this field does NOT appear in Employee mode | Additional field unique to Non-Employee path. Designation of the external deductor (required on Form 24Q). |

### Form Diff: Employee vs Non-Employee

| Field | Employee Mode | Non-Employee Mode |
|-------|--------------|-------------------|
| Deductor's Name | Combobox (lookup from Employee Master) | Free-text input |
| Deductor's Father's Name | Disabled (auto-fill expected) | Enabled free-text |
| Deductor's Designation | Hidden | Visible + enabled free-text |

---

## Actions & Interactions

| Action | Element | Pre-condition | Behaviour | Post-action |
|--------|---------|---------------|-----------|-------------|
| Save | Button ("Save") — always visible and enabled | None — button is enabled regardless of field state | Triggers client-side validation first; if PAN is empty or invalid format, shows error banner; if validation passes, fires `PUT /api/v1/settings/incometaxdetails` | On success (HTTP 200): presumably shows success toast (not directly captured — page remained on same URL). On validation failure: error banner appears at top of form with message list. |
| Close error banner | "Close" button (X icon) inside banner | Error banner visible | Dismisses the banner | Banner removed from DOM |
| Deductor Type toggle | Radio (Employee / Non-Employee) | Always available | Immediate form re-render below | Deductor Name changes between combobox and textbox; Father's Name enables/disables; Designation appears/hides |
| Deductor Name dropdown open | Click combobox | Employee mode selected | Fires `GET /api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll&search_text=` | Dropdown opens with search input (magnifier icon + textbox); shows "Sorry! No results found" when no employees exist |
| Hover AO code `i` icon | Mouse hover | Always | Tooltip appears inline (no click required) | Tooltip: AO code help text (see above) |
| Hover Tax Payment Frequency `i` icon | Mouse hover | Always | Tooltip appears inline | Tooltip: statutory payment deadline rule (see above) |
| Instant Helper | Button (top right) | Always available | Opens in-product walkthrough/help panel | Not audited in this session |

---

## Business Rules

### Statutory Rules

1. **TDS Payment Frequency is legislatively fixed at Monthly** for most corporate deductors under Section 200 of the Income Tax Act. The UI correctly represents this as a non-editable display field. Exception for March (deposit by 30th April) is correctly documented in the tooltip.

2. **PAN is mandatory and format-enforced.** Format: `^[A-Z]{5}[0-9]{4}[A-Z]{1}$` (10 chars, 5 alpha + 4 numeric + 1 alpha). This matches the Income Tax Department's PAN structure. Validation message: "Enter a valid PAN."

3. **TAN identifies the deductor** for all TDS challans and returns (Form 24Q for salary TDS, Form 26Q for non-salary). TAN format: `^[A-Z]{4}[0-9]{5}[A-Z]{1}$` (10 chars, 4 alpha + 5 numeric + 1 alpha). Marked optional in UI but functionally required before any TDS challan can be generated.

4. **AO (Assessing Officer) code** is required for TDS return filing (Form 24Q). It identifies the Income Tax circle governing the deductor. The 4-segment format corresponds to: `Area Code / AO Type / Range Code / AO Number`.

5. **Father's Name on Form 24Q** is a mandatory column in the TDS return. Zoho's design defers this by pulling it from the Employee Master record when Deductor Type is "Employee" — appropriate design that avoids data duplication.

6. **Deductor's Designation** is a Form 24Q requirement. It appears only in the Non-Employee path because for an Employee deductor, the designation is already captured in the Employee Master.

### Workflow Rules

- Save is **always enabled** — Zoho does not disable Save on empty/invalid state. Validation is triggered on click.
- Validation uses a **banner pattern** (top-of-form error list), not inline field highlighting. This means a user cannot easily pinpoint which field is invalid from the banner alone for multi-field validation errors.
- Successful save with a valid PAN+TAN but **no Deductor selected** proceeds without error (HTTP 200 returned for the test with `PAN=ABCDE1234F`, `TAN=MUMR12345A`, Deductor=empty). Deductor details appear to be optional at the settings level.
- Tax Payment Frequency **cannot be changed** from this UI. If a deductor qualifies for quarterly TDS payment (e.g., certain small deductors under Section 192), there is no mechanism to configure this — this is a potential functional gap.

---

## Validation Error Messages (Exact)

| Trigger | Banner Header | Error List Item |
|---------|---------------|-----------------|
| PAN empty on Save | "Oops! Looks like you missed something..." | "Enter a valid PAN." |
| PAN invalid format on Save | "Oops! Looks like you missed something..." | "Enter a valid PAN." |

Notes:
- Empty PAN and format-invalid PAN produce **identical error messages** — no distinction.
- No inline field highlighting observed — error is only in the banner.
- The banner has a dismissible "Close" (X) button.
- TAN format validation was not triggered in testing (TAN left empty on the successful save, invalid TAN format not separately tested — open question below).

---

## Data Relationships

| Entity | Relationship | Notes |
|--------|-------------|-------|
| Organisation / Tenant | 1:1 — this page stores one tax config per tenant | PAN + TAN are organisation-level identifiers |
| Employee Master | Many:1 — the Deductor's Name dropdown sources from Active employees | API: `GET /api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll` |
| Form 24Q / TDS Returns | This page feeds all required deductor fields into quarterly TDS return generation | TAN, PAN, AO code, Deductor Name, Father's Name, Designation are Form 24Q columns |
| Work Location / Branches | No direct relationship visible on this page | PT/ESI config is in Statutory Components, not here |

---

## API Contracts Observed

| Method | Endpoint | Trigger | Response |
|--------|----------|---------|----------|
| GET | `/api/v1/settings/incometaxdetails` | Page load | 200 — loads current org tax config |
| PUT | `/api/v1/settings/incometaxdetails` | Save button | 200 on success |
| GET | `/api/v1/autocomplete/employee?filter_by=Status.Active&employee_type=employee&module=Payroll&search_text=` | Deductor Name dropdown open | 200 — returns list of active employees |

---

## State & Status

This page has no record state machine. It is a single configuration record (1 per org). There are no Draft/Published/Approved states — it is saved directly.

The only observable state variation is the **conditional form rendering** based on Deductor's Type radio selection (Employee vs Non-Employee).

---

## Navigation

| Direction | Destination |
|-----------|-------------|
| Entry from | Settings sidebar > Taxes accordion > Tax Details link |
| Entry from | Onboarding checklist step (if applicable) |
| Exit to | Any other Settings page via sidebar |
| Exit to | "Close Settings" button (top right) returns to main app |

---

## Observations & Flags

### Critical Gaps

**[CRITICAL-01] Tax Payment Frequency is not configurable**
Some organisations may qualify for quarterly TDS deposit (e.g., deductors other than companies/co-operative societies with annual TDS liability below ₹15,000 — CBDT circular provision). The UI hardcodes "Monthly" with no override. This may be intentional for simplicity (most Indian corporates remit monthly) but should be explicitly confirmed as a design decision.

**[CRITICAL-02] TAN optionality allows partial save that cannot generate challans**
A save with valid PAN but no TAN returns HTTP 200 and persists. However, TAN is mandatory for Form 24Q filing and challan generation. There is no downstream guard visible at this stage — this could allow an org to run payroll and compute TDS without realising they cannot file returns or generate challans. Recommend: warn user (not block) when TAN is missing on save, especially after the first payroll run.

**[CRITICAL-03] AO code optionality — same concern**
AO code is required for Form 24Q. Saving without it is permitted. Like TAN, this will only surface as a gap at return-generation time, not at configuration time.

**[CRITICAL-04] PAN validation uses same error for empty and wrong format**
"Enter a valid PAN." is shown for both an empty PAN and a malformed one. An empty required field should ideally say "PAN is required" — conflating the two reduces debuggability for the user.

### Ambiguities

**[AMBIGUOUS-01] Father's Name auto-population from Employee Master**
When Deductor Type = Employee and a deductor is selected from the dropdown, does the Father's Name field auto-populate from the employee record? If Employee Master does not capture Father's Name, this field will remain empty even after selection. This is unverified because no employees exist in the test org.

**[AMBIGUOUS-02] Deductor's Name population in Employee mode**
In Employee mode, "Deductor's Name" is a searchable dropdown — but after selection, what name string is persisted? Employee's full name? Display name? Legal name as it appears on TDS return? This matters for Form 24Q accuracy.

**[AMBIGUOUS-03] TAN validation behaviour**
A save with `TAN=MUMR12345A` (which is a valid-format TAN) succeeded. Was a format-invalid TAN tested? The test only confirmed valid format succeeds. The exact TAN validation regex and error message are unconfirmed.

**[AMBIGUOUS-04] PAN input character cap**
Input of "INVALID123" (10 chars, valid length) appeared as "INVAL123" in the post-error snapshot. This may indicate the field caps input at 8 characters (not 10), or it could be a snapshot rendering artefact. The correct PAN maxlength is 10. Needs verification via DOM inspection with iframe access.

**[AMBIGUOUS-05] Successful save with no deductor — downstream impact**
Saving with PAN+TAN but no Deductor selected returns 200. Does the system allow payroll runs and TDS certificate generation without a named deductor? Form 24Q requires a responsible person to be named. A missing deductor would invalidate the return.

**[AMBIGUOUS-06] Quarterly TDS deductors**
If an org's annual TDS liability qualifies for quarterly remittance, is "Monthly" still enforced? Zoho may have intentionally restricted to monthly-only for V1 simplicity — this design choice should be confirmed.

### Well-Implemented

**[GOOD-01] AO code segmented input**
Splitting the AO code into 4 separate inputs (AAA/AA/000/00) maps exactly to the Income Tax Department's format. This reduces format errors compared to a single freeform field.

**[GOOD-02] Tax Payment Frequency tooltip**
The tooltip exactly states both the general rule (7th of following month) and the March exception (30th April). This is statutory-accurate and saves users from having to look it up.

**[GOOD-03] AO code tooltip with navigation path**
The tooltip for AO code directs users to exactly where they can find this number ("login into your Income Tax account and navigate to My Profile"). Practical and actionable help text.

**[GOOD-04] Deductor Type conditional form**
The Employee vs Non-Employee split is well-designed. For Employee deductors, the combobox pulls from live Employee Master (avoiding data duplication). For Non-Employee deductors, all fields are editable. The addition of Designation in the Non-Employee path aligns with Form 24Q requirements.

**[GOOD-05] Search-enabled Deductor combobox**
The live-search autocomplete against active employees (`search_text=` query param) is the right pattern for orgs with many employees. The "Sorry! No results found" empty state is clean.

---

## Open Questions

- [ ] Does Father's Name auto-populate from Employee Master when an employee is selected as deductor? Does Zoho's Employee Master capture Father's Name as a field?
- [ ] What is the exact TAN validation regex and its error message?
- [ ] Is the PAN field truly capped at 10 characters? Is the "INVAL123" truncation in the snapshot a rendering artefact or an actual input limit?
- [ ] Is Tax Payment Frequency ever configurable (quarterly option)? Is this an intentional scope limitation?
- [ ] Does a missing Deductor block Form 24Q generation or is it warned at generation time?
- [ ] Which user roles can access and edit this page? Is it OrgAdmin-only or also HRManager/PayrollManager?
- [ ] What data does the `PUT /api/v1/settings/incometaxdetails` request body contain? (Could not inspect due to cross-origin iframe blocking direct network body access.)
- [ ] Is there a separate "Tax Payment Frequency" for advance tax or is this strictly TDS-specific?
- [ ] What happens when settings are saved successfully — is there a success toast? (Not captured in this session — no visible toast was detected in the accessibility snapshot post-save.)

---

## Screenshots

| File | Description |
|------|-------------|
| `screenshots/03-tax-details-after-save.png` | Full page after successful save (PAN=ABCDE1234F, TAN=MUMR12345A) |
| `screenshots/03-tax-details-pan-validation-error.png` | Validation banner: "Oops! Looks like you missed something... Enter a valid PAN." |

---

## Next Session

**Resume from:** Settings > Statutory Components (`#/settings/statutory-details/list`)
**Pending on this page:** Test TAN format validation, test with a populated Employee Master to verify Father's Name auto-population, test role-based access.
**Recommended order after Statutory Components:** Pay Schedules > Salary Components > Work Locations
