# Item 100: Payslips Module — Full Audit

**Module:** Payslips (accessed via Employee Profile + Pay Run Summary)  
**No standalone route** — `#/payslips` returns 404. Payslips are embedded within:
1. Employee Profile → "Payslips & Forms" tab
2. Pay Run Summary → per-employee "Payslip" column + bulk "Send Payslip"
3. Settings → PDF Templates (payslip template management)

**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/100-payslips-and-forms-list.png` — Employee profile Payslips & Forms tab
- `screenshots/100-payslips-fy-dropdown.png` — Financial Year selector
- `screenshots/100-payslip-view-modal.png` — Payslip view modal (iframe PDF)
- `screenshots/100-payslip-download-dialog.png` — Download dialog (without password)
- `screenshots/100-payslip-download-password.png` — Download dialog (with password protection)
- `screenshots/100-send-payslip-modal.png` — Send Payslip bulk email confirmation modal
- `screenshots/100-payslip-pdf-templates.png` — PDF Templates settings (7 payslip templates)

---

## Section 1: Employee Profile — Payslips & Forms Tab

**URL:** `#/people/employees/{employeeId}/payslips-and-forms`  
**Entry Point:** Employee list → Employee row → "Payslips & Forms" tab

### Page Structure

Two sections:
1. **Payslips and TDS Sheets** — table of payslip + TDS sheet per pay run
2. **Form 16** — status of Form 16 generation for this employee

### Section 1A: Payslips and TDS Sheets

#### FY Selector

- Button label: "Financial Year: YYYY - YY"
- Dropdown with available financial years (only years with processed pay runs shown)
- Current: 2026 - 27 (contains May 2026 + April 2026 pay runs)

#### Table Columns

| Column | Description | Actions |
|--------|-------------|---------|
| PAYMENT DATE | Date pay run was paid (dd/MM/yyyy format) | — |
| MONTH | Pay run month (e.g., "May 2026") | — |
| PAYSLIPS | Per-payslip View + Download | "View" button (opens PDF modal) + icon button (opens Download dialog) |
| TDS SHEET | Monthly TDS worksheet | "View" button (opens PDF modal) + icon button (opens Download dialog) |

**Data observed (EMP001 Arjun Mehta):**
| Payment Date | Month | Payslip | TDS Sheet |
|-------------|-------|---------|-----------|
| 29/05/2026 | May 2026 | View + Download | View + Download |
| 30/04/2026 | April 2026 | View + Download | View + Download |

#### Payslip View Modal

- Title: "Payslip for {Month YYYY}"
- Content: iframe loading payslip PDF
- API endpoint: `/api/v1/employees/{employeeId}/payrollruns/{payrunId}/payslip?accept=pdf&print=true&organization_id={orgId}&frameorigin={origin}`
- Actions: **Print** (primary button) | **Close**
- No scrolling observed within modal — assumes single-page payslip

#### TDS Sheet View Modal

- Title: "TDS Sheet for {Month YYYY}"
- Content: iframe loading TDS worksheet PDF
- API endpoint: `/api/v1/employees/{employeeId}/taxworksheet?month={YYYY-MM}&accept=pdf&print=true&organization_id={orgId}&frameorigin={origin}`
- Actions: **Print** | **Close**

#### Download Payslip Dialog

**Title:** "Download Payslip"  
**Body:** "You can protect the payslip with a password to keep the data secure."

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Protect this file with a password | Checkbox | No | Unchecked | Toggles password field visibility |
| Password | Password input | Conditional | — | Only shown when checkbox checked. Placeholder: "Password must contain atleast 12 characters" |

**Validation:** Password minimum 12 characters (per placeholder text — actual validation not tested).

**Actions:** **Export** | **Cancel**

**Note:** The download icon button appears on both Payslips and TDS Sheet columns. Same "Download Payslip" dialog for both — may be the same endpoint with different `accept` type.

### Section 1B: Form 16

- Heading: "Form 16"
- Empty state: "Form 16 hasn't been generated for this employee yet!"
- Populated state: shows Form 16 Part A + Part B download links (from Form 16 module — see `form16/` docs)

### Employee Header Actions (on Payslips & Forms tab)

"Show dropdown menu" → Dropdown with:
- **Add / Update Vehicle Details** — adds/updates vehicle for perquisite valuation
- **Initiate Exit Process** — starts offboarding flow

**"Add" button** (top right): `data-test-selector="add-emp-salary-component"` — navigates to add salary component (context of Salary Details, carried across tab navigation).

---

## Section 2: Pay Run Summary — Payslip Actions

**URL:** `#/payruns/{payrunId}/summary`

### Per-Employee Payslip Column

Table header: PAYSLIP | TDS SHEET columns (visible in Paid state)

| Column | Available Actions |
|--------|-----------------|
| PAYSLIP | "View" button → opens Payslip modal (same as employee profile) |
| TDS SHEET | "View" button → opens TDS Sheet modal |

**Data observed (May 2026 pay run, Paid state):**
- EMP001 Arjun Mehta: View payslip + View TDS (29 Paid Days, ₹65,484.00)
- EMP002 Priya Sharma: View payslip + View TDS (31 Paid Days, ₹22,000.00)
- EMP003–005: SKIPPED (Onboarding incomplete) — no payslip available

### Bulk Send Payslip

**"Send Payslip" button** on pay run summary (visible in PAID state only):

**Confirmation modal:**
- Title: "Send Payslip"
- Re-send warning: "You've sent the payslips for this pay run already. Are you sure you want to send payslips again?"
- Body text: "The email sent to employees will contain a link that redirects them to the portal to view the payslip. For those without portal access, payslip will be available for download directly from the email."
- Actions: **Send** | **Cancel**

**Key rules from modal text:**
1. "Send Payslip" auto-sends when pay run transitions to Paid (first send is automatic).
2. Button on summary = re-send (with confirmation dialog to avoid accidental duplicate emails).
3. Email contains portal link if employee has portal access; otherwise direct download link in email.

---

## Section 3: PDF Templates Settings

**URL:** `#/settings/templates/regular-payslip`  
**Navigation:** Settings → Customisations (expandable) → PDF Templates → Regular Payslips

### Regular Payslips Templates

7 system templates:

| Template Name | Default | Edit URL |
|---------------|---------|----------|
| Elegant Template | Yes (DEFAULT badge) | `#/settings/templates/regular-payslip/{id}/edit` |
| Standard Template | No | `#/settings/templates/regular-payslip/{id}/edit` |
| Mini Template | No | `#/settings/templates/regular-payslip/{id}/edit` |
| Simple Template | No | `#/settings/templates/regular-payslip/{id}/edit` |
| Lite Template | No | `#/settings/templates/regular-payslip/{id}/edit` |
| Simple Spreadsheet Template | No | `#/settings/templates/regular-payslip/{id}/edit` |
| Professional Template | No | `#/settings/templates/regular-payslip/{id}/edit` |

### Per-Template Actions

| Action | Type | Behavior |
|--------|------|----------|
| Set as Default | Button | Sets this template as the org's default payslip format |
| Preview | Button (icon) | Opens template preview |
| Edit | Link (icon) | Navigates to template editor at `#/settings/templates/regular-payslip/{id}/edit` |

**Business Rule:** The "Elegant Template" is the DEFAULT. "Set as Default" button replaces the DEFAULT badge to the selected template. The default template is used for all payslip PDF generation (View + Download + Email).

### Other PDF Template Sub-sections

| Section | URL | Purpose |
|---------|-----|---------|
| Final Settlement Payslip | `#/settings/templates/final-settlement-payslip` | Separate template for F&F settlement payslips |
| Salary Certificate | `#/settings/templates/letter-templates/salary-certificate` | Letter template |
| Salary Revision Letter | `#/settings/templates/letter-templates/salary-revision` | Letter template |
| Bonus Letter | `#/settings/templates/letter-templates/bonus-letter` | Letter template |

---

## API Endpoints Discovered

| Endpoint | Method | Description |
|---------|--------|-------------|
| `/api/v1/employees/{id}/payrollruns/{runId}/payslip` | GET | Payslip PDF download |
| `/api/v1/employees/{id}/taxworksheet` | GET | TDS Sheet PDF |

**Query params for payslip:** `accept=pdf&print=true&organization_id={orgId}&frameorigin={origin}`  
**Query params for taxworksheet:** `month={YYYY-MM}&accept=pdf&print=true&organization_id={orgId}&frameorigin={origin}`

---

## Business Rules

1. **No standalone Payslips module** — payslips are accessed from employee profile or pay run summary.
2. **Payslip availability:** Generated only for non-skipped employees after pay run finalization.
3. **TDS Sheet** is a separate document from Payslip — it shows month-by-month TDS computation (useful for employees to track annual tax).
4. **Download password:** Optional PDF encryption. Minimum 12 characters. Applied at download time, not at generation.
5. **Automatic payslip send:** Zoho sends payslip emails automatically when pay run goes to Paid state. "Send Payslip" on summary = re-send with confirmation.
6. **Portal vs email access:** Employees with portal access get a link; without portal access, payslip is embedded/downloadable from email.
7. **FY filter on employee profile:** Shows only pay runs within the selected FY. Multiple FYs available if employee has been on payroll across FYs.
8. **Default template:** Elegant Template is the default. Only one default template at a time.
9. **7 regular payslip templates** + 1 Final Settlement template + 3 letter templates (Salary Certificate, Revision Letter, Bonus Letter).
10. **Skipped employees** in a pay run have no payslip for that period.

---

## Data Relationships

- Payslip → Pay Run (many-to-one): each payslip belongs to one pay run
- Payslip → Employee (many-to-one): each payslip belongs to one employee
- Payslip template → Org (many-to-one): templates are org-level; one is set as default
- TDS Sheet → Employee + Month (many-to-one): month-specific tax worksheet

---

## Cross-Module Impact

- Pay Run finalization → auto-generates payslips + sends emails
- Payslip download → uses active PDF template (default unless changed)
- Form 16 → separate flow in Taxes & Forms module, linked on employee Payslips & Forms tab
- Employee Portal → payslip view link in email depends on portal access setting

---

## Open Questions

- [ ] Can an employee download their own payslip from the self-service portal? What's the portal URL?
- [ ] Are payslips stored as pre-generated PDFs (S3/MinIO) or generated on-demand via the API?
- [ ] Can the download password be set at org level (so all payslips use the same password) rather than per-download?
- [ ] Is the TDS Sheet downloadable as well (not just viewable)? The icon button next to "View" in TDS Sheet column — does it also open a Download dialog?
- [ ] What happens to payslips when a pay run is revised? Are old payslips replaced or versioned?
- [ ] Can payslips be emailed to individual employees (not bulk) from the pay run or employee profile?
- [ ] Are there payslip-related reports (e.g., payslip delivery status — who opened the email)?
- [ ] What is the "Salary Transfer Letter" mentioned in Settings search — is it in the PDF Templates section?
