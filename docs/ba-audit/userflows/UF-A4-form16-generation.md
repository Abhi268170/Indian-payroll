# UF-A4: Form 16 Generation Flow

**Module:** Taxes & Forms > Form 16
**Tested:** 2026-05-16
**Approach:** Navigated to `#/taxes-and-forms/form16`. Fixed prerequisite: set Tax Deductor to Arjun Mehta (EMP001) via Settings > Tax Details. Explored Form 16 landing page and Generate Form 16 sub-page. Could not complete generation (trial org, no TRACES Part A ZIP available).

---

## Findings

### 1. Navigation

**Route:** `#/taxes-and-forms/form16`
**Entry points:**
- Left sidebar: Taxes & Forms → Form 16
- Pay run overflow dropdown: not available (Form 16 is FY-level, not pay-run-level)

---

### 2. Form 16 Landing Page (Pre-Generation)

**Page title:** "Form 16"
**URL:** `#/taxes-and-forms/form16`

**Page structure:**

| Section | Content |
|---------|---------|
| Header | "Form 16" page title + Financial Year dropdown (currently "Select") |
| Deductor card | "Verify your tax deductor" — shows name and parentage |
| Step indicator | 4-step horizontal flow |
| CTA button | "Generate Form 16" |
| Help section | "How to generate Form 16 for your employees?" + expandable steps |
| Secondary CTA | "Learn how to generate Form 16" (links to Zoho help article) |

**Financial Year Dropdown:**
- Label: "It's time to generate Form 16 for the financial year"
- Dropdown type: Custom Ember autocomplete-select (`.ac-selected`)
- Default value: "Select"
- Options: "Sorry! No results found" (trial org — no finalized FY data)
- Production expectation: Would show completed financial years e.g. "2024-25", "2025-26"

**Tax Deductor Verification Card:**
| Field | Value (test org) |
|-------|-----------------|
| Employee Name | Arjun Mehta |
| Parentage | Son / Daughter of Rajesh Mehta |
| Designation | Senior Software Engineer (EMP001) |

**Important note shown on page:**
> "Note: Remember that once you generate Form 16, you cannot change the deductor details."

This is a hard lock — deductor is frozen at Form 16 generation time. Irreversible action.

**4-Step Process Indicators:**

| Step | Label |
|------|-------|
| 1 | Upload Form 16 Part A |
| 2 | Generate Form 16 |
| 3 | Sign Form 16 |
| 4 | Publish/Email |

**Buttons:**
| Button | State | Action |
|--------|-------|--------|
| Generate Form 16 | Enabled (even without FY selected) | Navigates to `#/taxes-and-forms/form16/generate?fiscal_year=2026` |
| Learn how to generate Form 16 | Enabled | Opens Zoho help article (external link) |

---

### 3. Generate Form 16 Sub-Page

**URL:** `#/taxes-and-forms/form16/generate?fiscal_year=2026`

**Page title:** "Generate Form 16"
**Breadcrumb:** Form 16 > Generate Form 16

**Page structure:**

| Section | Content |
|---------|---------|
| Upload area | ZIP file upload for TRACES Part A |
| Upload button | "Upload and Generate" (disabled until file selected) |
| Instructions list | 7 numbered instructions |
| File format guidance | Must be TRACES-generated ZIP |

**File Upload Field:**
- Label: "Upload Form 16 Part A"
- File type: ZIP (TRACES-generated)
- Upload trigger: "Upload and Generate" button
- Pre-condition: File must be selected to enable Upload and Generate
- Note: The upload area has a drag-and-drop zone

**"Upload and Generate" Button:**
- Disabled state: When no file selected
- Enabled state: After file selection
- Post-action: Triggers async Form 16 Part B generation + merger with Part A

**7 Instructions shown on Generate Form 16 page:**

1. Download Form 16 Part A from TRACES (https://www.tdscpc.gov.in) for the relevant financial year.
2. The ZIP file downloaded from TRACES contains Part A for all employees.
3. **Part A should NOT be signed** before uploading — Zoho merges and then you sign after.
4. Zoho will generate Form 16 Part B automatically based on payroll data.
5. Zoho merges Part A + Part B into a single PDF per employee.
6. Form 12BA (perquisite statement) is included for eligible employees automatically.
7. Employees are matched by PAN — ensure employee PAN in Zoho matches TRACES records.

**Business Rules:**
- Part A: Source = TRACES ZIP upload (admin action)
- Part B: Source = Zoho-generated from payroll computation
- Merge: Zoho combines Part A + Part B into single employee-wise PDF
- Form 12BA: Auto-included for employees with perquisites (value > ₹1.5L threshold per Rule 26A)
- PAN matching: Employee PAN in Zoho master must match TRACES Part A exactly
- Signing: Done AFTER generation (Step 3 in process), not before upload

---

### 4. Prerequisites for Form 16 Generation

| Prerequisite | Status in test org |
|-------------|-------------------|
| Tax Deductor configured | Configured (Arjun Mehta, EMP001) |
| Financial Year with finalized payroll | Not met (trial — no complete FY) |
| TRACES Part A ZIP downloaded | Not tested |
| Employee PANs entered | Partial (EMP001 has PAN, EMP002 missing PAN) |
| TAN configured | Visible in Settings > Tax Details |

---

### 5. State Machine: Form 16 per Employee

```
Not Generated
    ↓ [Upload Part A + click "Upload and Generate"]
Part A Uploaded → Zoho generates Part B → Merge complete
    ↓
Generated (unsigned)
    ↓ [Admin signs — Step 3]
Signed
    ↓ [Publish or Email — Step 4]
Published / Emailed to Employee
```

**Publish options (Step 4 — not directly tested):**
- Expected: Publish to employee portal, Email to employee, Download ZIP

---

### 6. Form 16 Structure (Statutory Reference)

**Form 16 = Part A + Part B (Income Tax Rule 31)**

| Part | Source | Content |
|------|--------|---------|
| Part A | TRACES (government portal) | Certificate of tax deduction — TAN, PAN, quarterly TDS amounts, challan details |
| Part B | Employer-generated (Zoho) | Salary computation, exemptions, deductions, tax computation, chapter VI-A deductions |
| Form 12BA | Employer-generated | Perquisite statement for employees with non-monetary benefits |

**Statutory deadline:** 15 June following the financial year end (31 March). E.g., Form 16 for FY 2025-26 must be issued by 15 June 2026.

---

### 7. Tax Deductor Details (from Settings > Tax Details)

| Field | Value |
|-------|-------|
| Employee designation as deductor | Arjun Mehta (EMP001) |
| Father's Name | Rajesh Mehta (auto-populated from employee master) |
| Lock behaviour | Cannot be changed after Form 16 generation |

**Business rule:** Tax Deductor = the responsible signatory for TDS. This person's name and PAN appear on Form 16 Part A (after TRACES data merge) and Part B.

---

### 8. Navigation Paths

| From | To |
|------|----|
| Taxes & Forms sidebar | Form 16 landing |
| Form 16 landing | Generate Form 16 sub-page (via "Generate Form 16" button) |
| Generate Form 16 | Back to Form 16 landing (breadcrumb) |
| Form 16 landing | Zoho help article (external) |

---

## Screenshots / Files

- `form16-main-page.png` — Form 16 landing with deductor card and step indicators
- `settings-tax-details.png` — Tax Deductor configuration (prior session)
- `form16-page.png` — Earlier session screenshot
- `form16-generate-page.png` — Generate Form 16 sub-page (prior session)

---

## Gaps / Open Questions

- [ ] **FY dropdown options:** "Sorry! No results found" in trial org — what values appear in production? Expected: "2024-25", "2025-26".
- [ ] **Part A upload validation:** What happens if employee PANs in TRACES ZIP don't match Zoho employee master? Error per employee or whole upload fails?
- [ ] **Form 12BA threshold:** Exactly which employees get Form 12BA included? Is it based on perquisite value > ₹1,50,000 (Rule 26A) or any perquisite?
- [ ] **DSC vs manual signature:** Is Step 3 ("Sign Form 16") a Digital Signature Certificate (DSC) flow or a manual administrative sign-off within Zoho?
- [ ] **Email template for Form 16 delivery:** What does the Form 16 distribution email look like?
- [ ] **Re-generation:** If Form 16 is generated with errors, can it be regenerated? The deductor lock note suggests some irreversibility.
- [ ] **PAN missing for EMP002:** Priya Sharma has no PAN — she would be excluded from Form 16 or generate an error. 🔴
