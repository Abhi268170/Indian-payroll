# Employees > EMP004 — Contractor / Employment Type Gap (Aisha Khan)

## Employee Spec
- **Name:** Aisha Khan
- **Employee ID:** EMP004
- **Designation:** UX Consultant (created inline during wizard)
- **Department:** Design (created inline during wizard)
- **Work Location:** Head Office (Mumbai HQ)
- **Date of Joining:** 01/05/2025
- **Gender:** Female
- **DOB:** Not specified in spec (not entered)
- **Bank:** Axis Bank, A/C 912345678901, IFSC UTIB0001234, Savings
- **Gross CTC:** ₹7,20,000/year (₹60,000/month)
- **Spec Employment Type:** CONTRACTOR
- **Zoho Employee ID (internal):** 3848927000000034040

## Critical Gap: No Employment Type Field in Zoho

### Observation
Zoho Payroll's Add Employee wizard has **no Employment Type field** anywhere — not in Basic Details, not in Employment Details, not in any step. There is no way to designate an employee as a Contractor, Consultant, Freelancer, or Part-Time vs Full-Time within the Zoho Payroll employee profile.

### Implication
EMP004 (Aisha Khan) was created as a standard permanent employee even though the spec called for "CONTRACTOR" type. Designation "UX Consultant" was used to indicate the contractual nature, but this is a naming convention, not a system-enforced attribute.

### Why Employment Type Matters for Indian Payroll
| Scenario | Permanent Employee | Contractor |
|---|---|---|
| TDS mechanism | TDS u/s 192 (salary) | TDS u/s 194C/194J (professional fees) |
| PF applicability | Mandatory if salary < ₹15,000 (or org opted in) | Not applicable (not employer-employee relationship) |
| ESI applicability | Applicable if wage < ₹21,000 | Not applicable |
| PT applicability | Applicable (employer withholds) | Not applicable (contractor pays own PT) |
| Payslip type | Standard salary payslip | Fee invoice / vendor payment |
| Form 16 | Yes (employer issues) | No (contractor gets 26AS from TDS deductor) |

### Zoho's Approach
Zoho Payroll appears to only handle the employer-employee (salary) relationship. Contractor/vendor payments would be handled outside Zoho Payroll (e.g., via Zoho Books or manual payment). This is consistent with Zoho's product positioning as a payroll tool, not a contractor management platform.

### Our Build Decision
Our system is also V1 focused on employee payroll (salary, TDS u/s 192). Contractor payments (TDS u/s 194C/194J) are out of scope for V1. However, we should:
1. Include `employment_type` enum on Employee entity: `Permanent`, `Contract`, `Probation`, `Intern` — even if V1 only processes Permanent and Probation.
2. Gate statutory deductions on employment type — Contractors should not have PF/ESI auto-deducted.
3. Flag as `// DEFERRED: contractor-tds-194c` for 194C/194J TDS logic.

## Wizard Flow: Inline Creation of Department and Designation

### Department: "Design" (New)
- Triggered via "New Department" option in Department dropdown
- Modal appeared: single text field "Department Name" + Save
- "Design" department created and auto-selected
- Available for future employees immediately

### Designation: "UX Consultant" (New)
- Triggered via "New Designation" option in Designation dropdown
- Modal appeared: single text field "Designation" + Save
- "UX Consultant" created and auto-selected
- Available for future employees immediately

This confirms both Department and Designation support inline creation from the employee wizard — no need to pre-configure in Settings.

## Salary Structure (As Created)
- Annual CTC: ₹7,20,000
- Basic: 50% of CTC = ₹30,000/month (₹3,60,000/year)
- Fixed Allowance: ₹30,000/month residual (₹3,60,000/year)
- No HRA added

## IFSC Validation — Axis Bank
IFSC `UTIB0001234` — mock IFSC. Zoho lookup failed. Bank Name manually entered as "Axis Bank". Consistent with EMP002 and EMP003 patterns: all mock IFSCs require manual bank name entry.

## DOJ Save Issue (Observed During Creation)
EMP004 experienced the double-entry DOJ bug during creation (DOJ field had residual value from prior session attempt). Required clearing + re-entering + calendar click. This confirms the calendar-click requirement is consistent across all employees and sessions.

## Business Rules Observed
1. **No employment type in Zoho** — all employees treated as permanent salaried employees for statutory purposes.
2. **Inline Department creation** — creates org-level entity immediately; visible to all future employee creations.
3. **Inline Designation creation** — same pattern as Department.
4. **ESI non-applicability**: EMP004 ₹60,000/month > ₹21,000 ESI ceiling — not ESI eligible even if ESI were configured.

## Key Observations for Our Build
1. **`employment_type` enum required** — even if V1 behavior is identical for all types, the field must exist for future expansion. Add to Employee entity now.
2. **Contractor TDS (194C/194J) deferred** — mark clearly in codebase. Contractor employees should be visually flagged in UI but statutory deductions should not auto-apply.
3. **Department and Designation are org-level master data** — inline creation creates a shared entity. Our API must have `POST /departments` and `POST /designations` endpoints callable from the employee creation flow.
4. **Mock IFSC graceful fallback** — 3 out of 5 employees tested with mock IFSCs; all required manual bank name entry. Our IFSC lookup must degrade gracefully.

## Screenshots
- No specific screenshots for EMP004 beyond standard wizard steps already covered in 35–38.
