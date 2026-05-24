# V1 Build List — Indian Payroll (New Regime Only)

> Living document. Update as Zoho reference reveals missed flows.
> Scope: everything required to run payroll under new tax regime.
> Last updated: 2026-05-16

---

## OUT OF SCOPE (V1) — Read first

- Old regime IT declaration form
- POI submission and approval workflow
- 80C / 80D / 80G / HRA exemption inputs
- Old-regime TDS slabs
- Employee portal (payslip self-serve, declaration submission)
- Reimbursements module
- Giving / CSR module
- FBP (Flexible Benefit Plan)
- Approval workflows for pay runs (auto-approve in V1)
- Weekly / bi-weekly schedules
- Multi-entity support
- Automated bank transfer (NEFT / IMPS integration)
- Custom report builder
- Leave management / LOP sync (manual LOP entry only)
- Employee exit / F&F settlement
- Statutory Bonus automation

---

## MODULE 1: Org Structure

Prerequisites for employee creation. Simple CRUD for each.

### Screens
- Departments list + create modal + edit + delete (check dependents before delete)
- Designations list + create modal + edit + delete
- Work Locations (Branches) list + create modal + edit + delete
- Cost Centres list + create modal + edit + delete
- Business Units list + create modal + edit + delete

### Fields

**Work Location:** Name, State (IndianState enum), Address (optional)
**Department:** Name, Code (optional), Parent Department (optional)
**Designation:** Name
**Cost Centre:** Name, Code (optional)
**Business Unit:** Name, Code (optional)

### Rules
- Cannot delete if employees are assigned
- Work Location state drives PT and LWF computation for assigned employees

---

## MODULE 2: Settings — Salary Components

### Screens
- Components page with two tabs: Earnings | Deductions
- Create/Edit modal per type

### Earning formula types
| Type | Description |
|---|---|
| Fixed Amount | Flat ₹ per month |
| % of Basic | Percentage of Basic component |
| % of CTC | Percentage of annual CTC / 12 |
| Fixed Allowance | System component — absorbs residual CTC. One per structure. Auto-computed. |

### Fields
- Name, Code (unique per tenant), Type (Earning/Deduction), Formula Type
- Amount or Percentage (depends on formula type)
- Taxable flag (affects TDS computation)
- Pro-rata flag (affects proration on LOP/mid-month join)
- Is System Component (Fixed Allowance — non-deletable)

### Rules
- Cannot delete or change formula type if component is in an active salary structure
- Fixed Allowance: auto-computes as `CTC/12 − Σ all other components`; cannot be manually entered

---

## MODULE 3: Settings — Salary Structures

### Screens
- Structure list
- Structure builder (create/edit)

### Structure builder
- Add components from the earnings list
- Enter amount or % per component
- Fixed Allowance row auto-fills residual (live preview)
- Live CTC breakdown preview
- Save with a name (e.g., "Senior Engineer Band")

### Assignment
- Assign structure to employee with effective date
- Only one active structure per employee at a time
- Changing structure mid-month creates proration from effective date

---

## MODULE 4: Settings — Statutory Configuration

All values stored in DB config tables — zero hardcoded amounts.

### EPF
- Enable / disable org-wide
- Wage ceiling (default ₹15,000)
- Employee contribution rate: 12%
- Employer split: EPS 8.33% × min(wage, ceiling), EPF = 12% − EPS
- EDLI: 0.5% × min(wage, ceiling)
- Toggle: restrict contribution to ceiling (vs actual wage)

### ESI
- Enable / disable org-wide
- Gross threshold: ₹21,000 (employees above threshold are exempt)
- Employee rate: 0.75%
- Employer rate: 3.25%

### PT (Professional Tax)
- Per-state slab table (monthly gross → PT amount)
- Half-yearly deduction states: Karnataka, Maharashtra (Sept + Mar)
- Monthly deduction states: others
- Per-employee work location state determines applicable slab

### LWF (Labour Welfare Fund)
- Per-state amount (EE share + ER share)
- Frequency: monthly / half-yearly / annual (state-specific)
- Enable / disable per state

---

## MODULE 5: Settings — Pay Schedule

### Fields
- Frequency: Monthly only (V1)
- Pay Day: 1–31 or "Last working day of month"
- Pay Period: Calendar month

### Rules
- **Immutable after first pay run is processed** — show lock message in UI
- One pay schedule per tenant (V1)

---

## MODULE 6: Settings — Tax Details

Used for Form 16, Form 24Q, TDS worksheets.

### Fields
- Company PAN
- TAN (Tax Deduction Account Number)
- AO Code
- Deductor Name
- Deductor Type (Company / Individual / etc.)
- Responsible Person name + designation (signs Form 16)

---

## MODULE 7: Employees

### Screens
- Employee list (searchable, filterable by status/department)
- Employee profile (tabs: Overview, Salary, Statutory, Documents, Payroll)
- Add employee wizard (4 steps)
- Salary revision modal

### Add Wizard — Step 1: Personal
- First Name, Last Name, DOB, Gender, Mobile
- Personal Email
- PAN (mandatory — required for pay run inclusion)
- Aadhaar (optional, stored encrypted, displayed masked XXXX-XXXX-1234)

### Add Wizard — Step 2: Employment
- Employee Code (auto-generate or manual)
- Date of Joining
- Employment Type (Regular / Contract / Intern)
- Department, Designation
- Work Location (drives PT/LWF state)
- Cost Centre (optional)
- Work Email (used for portal login — future)

### Add Wizard — Step 3: Salary
- Select Salary Structure
- Enter CTC (annual)
- Components auto-populate; Fixed Allowance auto-computes
- Live monthly breakdown preview

### Add Wizard — Step 4: Statutory & Bank
- EPF: opt-out toggle, UAN (optional)
- ESI: eligibility auto-determined from gross ≤ ₹21,000; ESIC IP number (optional)
- PT: derived from Work Location state (display only)
- Bank Account Number (stored AES-256 encrypted)
- IFSC code (validate format; auto-fill bank name)
- Bank Name (auto-filled from IFSC lookup)

### Onboarding completeness gate
Required fields before employee appears in pay run:
- PAN
- Bank Account + IFSC
- Salary Structure assigned

Missing any → employee auto-skipped in pay run; skip reason shown in pay run UI.

### Employee profile tabs

**Overview:** Personal details, employment details, edit in place
**Salary:** Current structure, CTC, component breakdown, revision history
**Statutory:** Per-employee EPF/ESI/PT/LWF toggles (override org-level settings)
**Documents:** PAN card, Aadhaar, offer letter upload (stored in MinIO)
**Payroll:** List of past payslips (per pay run); download PDF per payslip

### Salary Revision
- Fields: New CTC, Effective Date, Reason
- If effective date falls within an already-closed pay run month → arrears flag (auto-create arrears run)
- Approval workflow: V1 auto-approve (no multi-level)

### Employee list
- Columns: Code, Name, Department, Designation, Status, CTC, Join Date
- Search by name / code
- Filter by: Status, Department, Designation
- Actions per row: View, Edit, Mark Inactive, Initiate Exit (deferred)
- Bulk import via CSV (with validation error report)

---

## MODULE 8: Payroll Engine (pure functions)

No I/O, no DI, no async. All inputs as parameters, all outputs as values. `decimal` only.

### Functions

| Function | Key Inputs | Output |
|---|---|---|
| `ComputeGross` | Salary structure, CTC, payable days, calendar days | Per-component amounts, total gross |
| `ComputeProration` | Component amount, payable days, calendar days | Prorated amount |
| `ComputePF` | Gross, wage ceiling, rates, opt-out flag | EE PF, ER PF, EPS, EDLI |
| `ComputeESI` | Gross, threshold, rates, eligible flag | EE ESI, ER ESI |
| `ComputePT` | Gross, state, month, year, slab table | PT amount |
| `ComputeLWF` | State, period, config table | LWF EE, LWF ER |
| `ComputeTDS` | Annual gross projection, standard deduction, tax slabs, rebate config, prior employer YTD | Monthly TDS to deduct |
| `ComputeNetPay` | Gross, all deduction amounts | Net pay |
| `ComputeArrears` | Old structure amounts, new structure amounts, months to backfill | Arrears amount per month |
| `ComputeLoanPerquisite` | Loan balance, SBI benchmark rate, loan interest rate | Perquisite value (taxable) |

### TDS — New Regime Slabs (FY 2026-27)

| Income Slab | Rate |
|---|---|
| ₹0 – ₹3,00,000 | 0% |
| ₹3,00,001 – ₹7,00,000 | 5% |
| ₹7,00,001 – ₹10,00,000 | 10% |
| ₹10,00,001 – ₹12,00,000 | 15% |
| ₹12,00,001 – ₹15,00,000 | 20% |
| Above ₹15,00,000 | 30% |

- Standard Deduction: ₹75,000
- Section 87A rebate: if tax ≤ ₹25,000 AND income ≤ ₹12,00,000 → net tax = ₹0
- Surcharge: not applicable for salaried V1
- Monthly TDS = (annual projected tax − tax already deducted YTD) / remaining months

### Proration formula
```
prorated_amount = component_amount × payable_days / calendar_days_in_month
```
Payable days = calendar days − LOP days (not working days).

---

## MODULE 9: Pay Run

### Pay run types (V1)
- Regular Monthly
- Arrears
- Bonus
- Off-cycle (one-off payment)

### State machine
```
Draft → UnderReview → Approved → PaymentDue → Paid
                                              ↓ (reversal)
                                         PaymentDue
```
- Reprocess: unlock approved/paid run → back to Draft (with reason)
- Immutable once Paid (except reversal)

### Regular Pay Run — full flow

1. **Create** — select month/year, run type
2. **Employee load** — all active, onboarding-complete employees auto-included
   - Incomplete employees shown in "Skipped" section with reason
   - New joiners: payable days auto-calculated from DOJ
3. **Variable inputs** — per employee:
   - LOP days (reduces payable days)
   - One-time earnings (bonus, reimbursement payout)
   - TDS override (manual TDS amount)
4. **Preview / Draft** — computed breakdown per employee
5. **Summary tabs:**
   - Employee Summary: Name, Gross, Deductions, Net Pay, Status
   - Taxes & Deductions: EPF, ESI, PT, LWF, TDS per employee
   - Overall Insights: total payroll cost, total statutory, total net
6. **Finalize** → Approved state
7. **Record Payment** → enter payment date, payment mode → Paid

### Pay run screens
- Pay run list (status badge, month, type, employee count, total net)
- Pay run detail (3-tab summary)
- Per-employee slide-in panel (full payslip breakdown)
- Skip employees modal (show reason)
- LOP entry modal
- Variable pay modal (one-time earnings)
- Record payment modal (date + mode)
- Reverse payment modal (reason)

### Payslip PDF
- AES-256 encrypted (optional password protection)
- Bank account masked in PDF: XXXX1234 (not plaintext)
- Sections: Earnings table, Deductions table, Net Pay, Employer Contributions, YTD
- Bulk download: ZIP of all employee payslips

### Bank Advice
- Generated post-finalize
- Columns: Employee Name, Bank Name, Account Number (full — for actual transfer), IFSC, Net Pay
- Format variants: Generic CSV, SBI, HDFC, ICICI, Axis (minimum)
- UI shows account masked; file contains full account for bank

### TDS Worksheet per employee
- Downloadable PDF (not print-only like Zoho)
- Shows: gross projection, standard deduction, taxable income, slab-wise tax, 87A rebate, net tax, monthly TDS
- Correct statutory citation: Section 87A (not "Section 156")

---

## MODULE 10: Statutory Compliance Files

| File | Trigger | Format | Notes |
|---|---|---|---|
| EPF ECR | Post pay run finalize | ECR2 text (EPFO spec) | UAN-wise contribution |
| ESI Monthly Return | Monthly | ESIC Excel/CSV format | IP-wise contribution |
| PT Challan | State frequency | PDF challan | Per-state format |
| LWF Challan | Half-yearly / annual | Per-state format | |
| Form 24Q | Quarterly | .fvu text file (TRACES spec) | Annexure I + II |
| Form 16 Part B | End of FY | PDF | Employer generates, signs, emails |

### TDS Liabilities page
- Month-wise TDS payable summary
- Record challan (ITNS 281): BSR code, date, amount, serial number
- Associate challan to quarter for Form 24Q

### Form 24Q
- Annexure I: deductee-wise (employee PAN, name, gross, TDS)
- Annexure II: challan-wise (BSR code, date, amount, TAN)
- Generate .fvu file for TRACES upload

### Form 16
- Part A: downloaded from TRACES (admin uploads XML)
- Part B: generated by system (employer fills gross, deductions, net tax)
- Merge Part A + Part B into final PDF
- Sign with digital signature (or manual sign flow)
- Publish to employees (email)

---

## MODULE 11: Loans (P1)

### Screens
- Loan list (per employee or org-wide)
- Loan detail + repayment schedule table

### Loan create fields
- Employee, Loan Type, Principal Amount, Interest Rate (% p.a.)
- EMI Amount, Start Month, Disbursement Date

### Loan actions
- Pause EMI (skips deduction for selected months)
- Record manual repayment (reduces balance outside pay run)
- Foreclosure (close loan with lump sum)
- Delete (only if no EMI deducted yet)

### Loan perquisite
- Computed if loan interest rate < SBI benchmark rate (Rule 15(5))
- Perquisite value = (SBI rate − loan rate) × outstanding balance / 12
- Added to taxable income for TDS computation

---

## MODULE 12: Reports

All reports: XLSX + CSV export. Payslip/Form 16/TDS Worksheet: PDF.

| Report | Category |
|---|---|
| Payroll Summary | Payroll |
| CTC Breakup | Payroll |
| Salary Register | Payroll |
| YTD Report | Payroll |
| Bank Advice Report | Payroll |
| New Joiners Report | HR |
| Employee Master | HR |
| Resigned / Exited | HR |
| EPF Report | Statutory |
| ESI Report | Statutory |
| PT Report | Statutory |
| LWF Report | Statutory |
| TDS Liability Report | Tax |
| Loan Outstanding Report | Loans |

---

## Summary Counts

| Area | Count |
|---|---|
| Settings screens | 6 (org structure ×4 + pay schedule + tax details) |
| Salary config screens | 2 (components + structures) |
| Statutory config screens | 4 (EPF, ESI, PT, LWF) |
| Employee screens | 5 (list, profile ×5 tabs, wizard, revision) |
| Pay run screens | 6 (list, detail, slide-in, 4 modals) |
| Statutory files screens | 4 (TDS liabilities, Form 24Q, Form 16, ECR/ESI/PT/LWF) |
| Loan screens | 2 |
| Reports screen | 1 (14 reports) |
| **Total screens** | **~30** |
| Engine functions | 10 |
| API endpoint groups | ~15 |
| Forms / modals | ~35 |

---

## Build Order (dependency-driven)

```
1. Org Structure (Dept, Designation, Work Location, Cost Centre)
2. Salary Components
3. Salary Structures
4. Statutory Config (EPF, ESI, PT, LWF)
5. Pay Schedule + Tax Details
6. Employee (full wizard + profile)
7. Payroll Engine (expand calculators)
8. Pay Run (core loop)
9. Payslip PDF + Bank Advice
10. Statutory Files (ECR, ESI, PT, LWF, Form 24Q)
11. Form 16
12. Loans
13. Reports
```
