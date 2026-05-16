# Zoho Payroll — Master User Flows Compiled Reference

**Compiled from:** 102 audit files (UF-01 through UF-96 + DS-01 through DS-06)
**Audit date:** 2026-05-16
**Reference product:** Zoho Payroll (zoho.com/in/payroll)
**Demo org:** lerno (Kerala, Thrissur)
**Admin account:** abhijithss2255@gmail.com
**Employees audited:** Arjun Mehta (EMP001), Priya Sharma (EMP002), Vikram Nair (EMP003), Aisha Khan (EMP004), Rahul Verma (EMP005?)

---

## Contents

1. [System Setup and Onboarding](#1-system-setup-and-onboarding)
2. [Salary Components Configuration](#2-salary-components-configuration)
3. [Statutory Configuration](#3-statutory-configuration)
4. [Approval Workflows](#4-approval-workflows)
5. [Employee Management](#5-employee-management)
6. [IT Declaration and TDS](#6-it-declaration-and-tds)
7. [Employee Exit and Gratuity](#7-employee-exit-and-gratuity)
8. [Pay Runs — Regular Cycle](#8-pay-runs--regular-cycle)
9. [Pay Runs — Special Types](#9-pay-runs--special-types)
10. [Approvals Module](#10-approvals-module)
11. [Loans Module](#11-loans-module)
12. [Taxes and Statutory Forms](#12-taxes-and-statutory-forms)
13. [Reports Centre](#13-reports-centre)
14. [Employee Portal](#14-employee-portal)
15. [Additional Settings](#15-additional-settings)
16. [Design System](#16-design-system)

**Appendices:**
- [A. Calculation Reference](#appendix-a-calculation-reference)
- [B. Cross-Module Data Flow](#appendix-b-cross-module-data-flow)
- [C. Gaps and Build Recommendations](#appendix-c-gaps-and-build-recommendations)
- [D. Key Entities and Fields](#appendix-d-key-entities-and-fields)

---

## 1. System Setup and Onboarding

### 1.1 Getting Started Wizard (UF-01, UF-02)

**URL:** `#/home/dashboard`

The dashboard shows a 7-step onboarding checklist. In the demo org, 5 of 7 steps are complete.

| Step | Status | URL |
|------|--------|-----|
| 1. Add Organisation Details | Completed | `#/settings/orgprofile` |
| 2. Provide your Tax Details | Completed | `#/settings/taxes` |
| 3. Configure your Pay Schedule | Completed | `#/settings/pay-schedules` |
| 4. Set up Statutory Components | NOT Completed | `#/settings/statutory-details/list` |
| 5. Set up Salary Components | NOT Completed | `#/settings/salary-components/earnings` |
| 6. Add Employees | Completed | `#/people/employees` |
| 7. Configure Prior Payroll | Marked Complete (misleading) | `#/prior-payroll` |

**Critical observation:** Step 7 is marked complete as soon as the admin visits the page — not based on actual configuration. Prior Payroll is NOT enabled. This is a false signal.

**Manual override:** Each step has a "Mark as Completed" button allowing admin to skip steps. This bypasses validation — a potential compliance risk.

**Additional Features section (dashboard):**
- Direct Deposit (ICICI/HSBC integration)
- Salary Templates
- IT Declaration reminders
- Employee Custom Fields

### 1.2 Prior Payroll Configuration

**URL:** `#/prior-payroll`

**Current state:** Not enabled. If enabled, admin can import per-employee YTD data:
- Prior employer name and TAN
- Salary paid by prior employer
- TDS deducted by prior employer
- PF contributions by prior employer

**Why it matters:** Without prior payroll data, TDS computation is based only on Zoho-period salary. Employees who joined mid-year with income at a previous employer will have understated TDS, creating a tax shortfall at year-end.

**Business rule:** Statutory requirement under Form 12B — employer must account for prior employer income when computing TDS.

---

## 2. Salary Components Configuration

### 2.1 Earnings Components (UF-03, UF-06, UF-07, UF-86)

**URL:** `#/settings/salary-components/earnings`

**Sub-tabs:** Earnings | Deductions | Benefits | Reimbursements

#### All Earnings Components (15 total)

| Name | Earning Type | Calculation Type | EPF | ESI | Status |
|------|-------------|-----------------|-----|-----|--------|
| Basic | Basic | Fixed; 50% of CTC | Yes | Yes | Active |
| House Rent Allowance | House Rent Allowance | Fixed; 50% of Basic | No | Yes | Active |
| Conveyance Allowance | Conveyance Allowance | Fixed; Flat Amount | Yes (if PF wage < 15k) | No | Active |
| Children Education Allowance | Children Education Allowance | Fixed; Flat Amount | Yes (if PF wage < 15k) | Yes | Inactive |
| Transport Allowance | Transport Allowance | Fixed; ₹1,600/month | Yes (if PF wage < 15k) | Yes | Inactive |
| Travelling Allowance | Travelling Allowance | Fixed; Flat Amount | Yes (if PF wage < 15k) | No | Inactive |
| Special Allowance | Custom Allowance | Fixed; Flat Amount | No | Yes | Active |
| Fixed Allowance | Fixed Allowance | Fixed; Flat Amount | Yes (if PF wage < 15k) | Yes | Active |
| Overtime Allowance | Overtime Allowance | Variable; Flat Amount | No | Yes | Inactive |
| Gratuity | Gratuity | Variable; Flat Amount | No | No | Active |
| Bonus | Bonus | Variable; Flat Amount | No | No | Active |
| Commission | Commission | Variable; Flat Amount | No | Yes | Active |
| Leave Encashment | Leave Encashment | Variable; Flat Amount | No | No | Active |
| Notice Pay | Notice Pay | Variable; Flat Amount | No | No | Active |
| Hold Salary | Hold Salary (Non Taxable) | Variable; Flat Amount | No | No | Active |

#### EPF Wage "If PF Wage < 15k" Rule

The conditional EPF flag means: if total EPF wage is already at or above ₹15,000/month, these components are excluded from PF wage computation. This reflects the statutory ceiling — PF is computed on a maximum wage of ₹15,000/month.

#### Component Taxability

| Component | Tax Treatment |
|-----------|--------------|
| Basic | Fully taxable |
| HRA | New regime: fully taxable. Old regime: exempt per Section 10(13A) formula |
| Conveyance | New regime: taxable. Old regime: partially exempt |
| Transport Allowance | New regime: taxable. ₹1,600/month exempt in old regime |
| Special Allowance | Fully taxable |
| Fixed Allowance | Fully taxable |
| Gratuity | Up to ₹20L exempt for private sector employees |
| Bonus | Fully taxable |
| Leave Encashment | Exempt on retirement; taxable if encashed during service |
| Notice Pay | Taxable (received); deductible (paid by employee) |
| Hold Salary | Labeled "Non Taxable" — treated as advance/hold |

### 2.2 Create Earnings Component — Form Fields (UF-03)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Earning Type | Dropdown (33 types) | Yes | Fixed post-creation |
| Component Name | Text | Yes | Display name |
| Name in Payslip | Text | No | Override name on payslip |
| Pay Type | Radio | Yes | Fixed / Variable |
| Calculation Type | Radio | Yes | % of CTC, % of Basic, Flat Amount |
| Consider for EPF | Toggle | Yes | Included in PF wage? |
| Consider for ESI | Toggle | Yes | Included in ESI wage? |
| Pro-rata (LOP) | Toggle | Yes | Deducted on LOP? |
| Active | Toggle | Yes | Active on creation |

**Pre-built components (14 shown during creation):** Basic, HRA, Conveyance, Children Education, Transport, Travelling, LTA, Special Allowance, Fixed Allowance, NPS, Overtime, Car Allowance, Telephone, Meal Allowance.

### 2.3 Deductions Components

| Name | Deduction Type | Frequency | Status |
|------|---------------|-----------|--------|
| Meal Card | Other Deductions | Recurring | Active |
| Withheld Salary | Withheld Salary | One Time | Active |
| Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

**Note:** Statutory deductions (EPF, ESI, PT, TDS, LWF) are system-managed — they do not appear here. They are computed automatically per pay run.

**Create Deduction form fields (UF-04):** Name, Frequency (Recurring/One Time), Active toggle. The form does NOT have pre-tax/post-tax distinction or maximum amount setting — amount is set at employee level.

### 2.4 Benefits Components

| Name | Benefit Type | Frequency | Status |
|------|-------------|-----------|--------|
| Voluntary Provident Fund | Voluntary Provident Fund | Recurring | Active |

**VPF rules:** Employee contributes additional amount beyond mandatory 12% EPF. Earns same interest as EPF (8.25% p.a.). Employer does NOT match VPF contributions. In new regime: no 80C benefit but interest still earned.

### 2.5 Reimbursements Components

| Name | Reimbursement Type | Max Amount | Status |
|------|--------------------|-----------|--------|
| Fuel Reimbursement | Fuel Reimbursement | ₹0 | Inactive |
| Driver Reimbursement | Driver Reimbursement | ₹0 | Inactive |
| Vehicle Maintenance Reimbursement | Vehicle Maintenance Reimbursement | ₹0 | Inactive |
| Telephone Reimbursement | Telephone Reimbursement | ₹0 | Inactive |
| Leave Travel Allowance | Leave Travel Allowance | ₹0 | Inactive |

All 5 are inactive with ₹0 maximum — no reimbursement claims can be submitted. FBP feature is consequently inactive.

### 2.6 Salary Templates / Structures (UF-05, UF-87)

**URL:** `#/settings/salary-templates`

One template configured: **Standard-Exec**

#### Standard-Exec Template Structure

| Component | Percentage/Amount | Notes |
|-----------|------------------|-------|
| Basic | 50% of CTC | Variable — changes with CTC |
| HRA | 50% of Basic | = 25% of CTC |
| Special Allowance | ₹0 (flat) | Optional add-on |
| Fixed Allowance | Residual absorber | = CTC − Basic − HRA − all others |

**Fixed Allowance = Residual:** This is the core mechanism of Zoho's salary structure. Fixed Allowance absorbs any remainder after other components are deducted from CTC. It cannot be set as a percentage — it is always computed as CTC − sum(all other components).

**For Arjun Mehta, CTC ₹8,40,000/year = ₹70,000/month:**

| Component | Monthly | Annual |
|-----------|---------|--------|
| Basic | ₹40,000 (57.14%) | ₹4,80,000 |
| HRA | ₹16,000 (40% of Basic) | ₹1,92,000 |
| Fixed Allowance | ₹14,000 (residual) | ₹1,68,000 |
| Total CTC | ₹70,000 | ₹8,40,000 |

**Template immutability rules:**
- Earning Type cannot be changed after creation
- EPF/ESI/pro-rata flags lock once a component is associated with an employee
- Amount changes apply only to new assignments, not retroactively
- Template changes do NOT retroactively affect existing employee assignments

### 2.7 Edit/Delete Component Constraints (UF-06, UF-07)

**Editable post-creation:** Name, Name in Payslip, Amount (for new employees only)
**Always locked:** Earning Type (immutable from creation)
**Locked once associated:** EPF flag, ESI flag, Pro-rata flag

**Delete:** Hard delete, irreversible ("cannot be undone"). No association check before delete — no guard preventing deletion of an in-use component. "Mark as Inactive" is the safer alternative (not suggested by UI).

---

## 3. Statutory Configuration

### 3.1 EPF Configuration (UF-08)

**URL:** `#/settings/statutory-details/epf`
**Demo EPF Number:** KA/KAR/1234567/001 (Karnataka prefix — mismatch with Kerala location)

#### EPF Contribution Rates

| Contribution | Rate | Basis | Cap |
|-------------|------|-------|-----|
| Employee (EE) EPF | 12% | PF Wage | No absolute cap (but wage ceiling applies) |
| Employer (ER) EPS | 8.33% | PF Wage | Capped at ₹15,000 wage ceiling = ₹1,250/month |
| Employer (ER) EPF | 12% − EPS | PF Wage | ER EPF = Total 12% − EPS portion |
| EDLI | 0.5% | PF Wage | Capped at ₹15,000 wage ceiling = ₹75/month |

**Configurable options observed:**
- Employee contribution: Based on actual PF wage or restricted to ₹15,000 ceiling
- Employer contribution: Based on actual PF wage or restricted to ₹15,000 ceiling
- ABRY (Atmanirbhar Bharat Rojgar Yojana) scheme visible in read view — admin cannot modify
- LOP handling: Whether PF is computed on actual days paid or full month

**Demo org:** All employees have EPF = Disabled (per-employee toggle). EPF Number has Karnataka prefix (KA/KAR) but org is in Kerala — flagged as configuration mismatch.

#### Expected EPF Calculation (if Arjun EPF were Enabled)

| Item | Calculation | Amount |
|------|-------------|--------|
| Basic (PF Wage) | ₹40,000 (> ₹15,000 ceiling) | PF computed on ₹15,000 |
| EE EPF (12%) | 12% × ₹15,000 | ₹1,800 |
| ER EPS (8.33%) | 8.33% × ₹15,000 | ₹1,250 (capped) |
| ER EPF | 12% − 8.33% = 3.67% × ₹15,000 | ₹550 |
| EDLI (0.5%) | 0.5% × ₹15,000 | ₹75 |

### 3.2 ESI Configuration (UF-09)

**URL:** `#/settings/statutory-details/esi`
**Demo ESI Number:** 52-00-123456-000-0001 (52 = Kerala region code)

| Contribution | Rate | Basis |
|-------------|------|-------|
| Employee (EE) | 0.75% | Gross ESI Wage |
| Employer (ER) | 3.25% | Gross ESI Wage |

**ESI Wage Ceiling:** ₹21,000/month gross. Employees earning above ₹21,000 are not covered.

**Contribution periods:** April–September and October–March. Once an employee is covered in a period, they continue to be covered for the full 6-month period even if their salary crosses ₹21,000 during the period.

**Demo org:** Arjun (₹70,000) and Priya (₹22,000) both exceed ₹21,000 — correctly excluded. All ESI = ₹0 in May 2026.

### 3.3 Professional Tax (UF-10, UF-71)

**URL:** `#/settings/statutory-details/pt`
**Configured at:** Work location level (not org-level)

#### Kerala PT Slabs (Half-Yearly)

| Monthly Gross Range | Half-Yearly PT |
|--------------------|----------------|
| Up to ₹11,999 | ₹0 |
| ₹12,000 – ₹17,999 | ₹320 |
| ₹18,000 – ₹29,999 | ₹450 |
| ₹30,000 – ₹44,999 | ₹600 |
| ₹45,000 – ₹99,999 | ₹750 |
| ₹1,00,000 – ₹1,24,999 | ₹1,000 |
| ₹1,25,000 and above | ₹1,250 |

**Kerala PT cycle:** Deducted in September and March only. May 2026 PT = ₹0 (correct — not a PT deduction month).

**Expected September 2026:** Arjun (₹70,000/month) → ₹750; Priya (₹22,000/month) → ₹450.

**PT Number:** Blank in demo org. Without PT Number, PT challan generation is blocked.

**"(Revise)" button:** Allows admin to override the system-default slab values for a specific work location.

#### Karnataka PT (for reference)

| Monthly Gross | Monthly PT |
|--------------|-----------|
| Up to ₹14,999 | ₹0 |
| ₹15,000 and above | ₹200 |

### 3.4 LWF Configuration (UF-11, UF-72)

**URL:** `#/settings/statutory-details/lwf`

**Kerala LWF:**
- Employee contribution: ₹50/month
- Employer contribution: ₹50/month
- Frequency: Monthly

**Demo org status:** LWF was Disabled. Enabling requires a confirmation dialog.

**No per-employee LWF opt-out** — once enabled at org level, applies to all Kerala employees.

**LWF Registration Number:** Not visible in the settings form — unclear if required for challan generation.

#### State-wise LWF Comparison

| State | EE Contribution | ER Contribution | Frequency |
|-------|----------------|----------------|-----------|
| Kerala | ₹50/month | ₹50/month | Monthly |
| Maharashtra | ₹6/month (> ₹3,000 salary) | ₹12/month | Monthly |
| Karnataka | ₹20/6 months | ₹40/6 months | June and December |
| Tamil Nadu | ₹10/6 months | ₹20/6 months | June and December |
| Gujarat | Annual | Annual | December |
| Delhi | ₹5/6 months | ₹10/6 months | June and December |

---

## 4. Approval Workflows

### 4.1 Workflow Types (UF-12, UF-13, UF-96)

Three types available for Salary Revision, Reimbursements, and Pay Runs:

| Type | Description | Use Case |
|------|-------------|----------|
| Simple Approval | Any authorized user with approval permission can approve | Small orgs, single admin |
| Multi-Level Approval | Sequential chain; ALL approvers must approve | Governance-heavy orgs |
| Custom Approval | Criteria-based routing | Enterprises; e.g., route high-value runs to CFO |

**Configuration location:**
- Salary Revision approval: `#/settings/salary-revision/custom-approval/list`
- Pay Run approval: `#/settings/payrun/custom-approval/list`
- Reimbursement approval: `#/approvals/reimbursements` (approval settings embedded)

### 4.2 Salary Revision Approval (UF-12)

- User-specific approvers (not role-based)
- No escalation timer configured
- Pending revision banner appears on employee's Salary Details tab
- Arjun's revision: ₹8,40,000 → ₹9,45,000 (13% increase), effective June 2026, Status: Pending

### 4.3 Pay Run Approval Tabs (UF-13, UF-96)

Beyond the workflow type radio, Pay Run settings has additional tabs:

| Tab | URL | Description |
|-----|-----|-------------|
| Approvals | `/custom-approval/list` | Workflow selection |
| Custom Button | `/custom-button/list` | Add custom action buttons to pay run UI |
| Record Locking | `/record-locking` | When pay run records become immutable |
| Related List | `/related-list` | Related entity lists in pay run detail |

---

## 5. Employee Management

### 5.1 Add Employee — Wizard (UF-14)

**Entry point:** Employees page → "New" button
**Flow:** 4-step wizard

**Step 1: Basic Details**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Employee ID | Text (auto) | Auto | EMP001, EMP002 format; mutable post-creation (risk) |
| First Name | Text | Yes | |
| Last Name | Text | No | |
| Date of Joining | Date (dd/MM/yyyy) | Yes | |
| Work Email | Email | Yes | Must be unique |
| Gender | Dropdown | Yes | Male/Female/Other |
| Work Location | Dropdown | Yes | Determines PT slab |
| Designation | Text/combobox | Yes | Inline create possible |
| Department | Text/combobox | Yes | Inline create possible |
| Pay Schedule | Dropdown | Yes | Linked to configured schedules |

**Step 2: Salary Details**
- CTC entry, template selection, component percentages
- Basic %, HRA %, Fixed Allowance (auto-computed residual)
- Effective From date

**Step 3: Personal Details**
- DOB, Father's Name, PAN, Mobile, Personal Email, Permanent Address
- These fields determine payroll readiness (onboarding gate)

**Step 4: Payment Information**
- Bank Account Number, IFSC, Account Type, Payment Mode

**Statutory settings** (EPF/ESI/PT/LWF toggles) are NOT part of the wizard — they are accessed from the Overview tab after employee creation.

### 5.2 Employee Profile Tabs

| Tab | Content |
|-----|---------|
| Overview | Basic info, statutory toggles (EPF/ESI/PT/LWF), Employment details |
| Salary Details | CTC breakdown, pending revision banner, "Revise" button |
| Investments | IT Declaration sections, regime selection |
| Payslips & Forms | Historical payslips, TDS worksheets |
| Loans | Employee-specific loan list |

### 5.3 Employee Roster (Demo Org)

| Emp ID | Name | Role | CTC/month | DOJ | EPF | ESI | PT | LWF | Status |
|--------|------|------|----------|-----|-----|-----|----|----|--------|
| EMP001 | Arjun Mehta | Senior Engineer | ₹70,000 | Unknown | Disabled | Disabled | Enabled | Disabled | Active |
| EMP002 | Priya Sharma | HR Manager | ₹22,000 | Unknown | Disabled | Disabled | Disabled | Disabled | Active |
| EMP003 | Vikram Nair | Unknown | ₹1,50,000/mo | 01/01/2025 | Disabled | Disabled | Unknown | Unknown | Skipped |
| EMP004 | Aisha Khan | Unknown | Unknown | 01/05/2025 | Disabled | Disabled | Unknown | Unknown | Skipped |
| EMP005 | Rahul Verma | Unknown | Unknown | Unknown | Unknown | Unknown | Unknown | Unknown | Unknown |

### 5.4 Payroll Readiness Gate (UF-41, UF-49)

Employees are skipped from pay runs if any of these fields are missing:

| Required Field | Arjun | Priya | Vikram | Aisha |
|---------------|-------|-------|--------|-------|
| Date of Birth | Present | Present | Missing | Missing |
| Father's Name | Present | Present | Missing | Missing |
| Personal Email | Present | Present | Missing | Missing |
| Permanent Address | Present | Present | Missing | Missing |
| Bank Account | Present | Present | Missing | Missing |
| PAN | Present | "-" (but included) | Missing | Missing |
| Salary Structure | Assigned | Assigned | Assigned | Assigned |

**Key nuance:** Priya has PAN = "-" but has DOB, Father's Name, Address, and Bank — she is INCLUDED in pay runs. The gate is composite (all required fields), not PAN-only. Vikram and Aisha are skipped because nearly all personal fields are blank.

**No override mechanism:** Admin cannot force-include a skipped employee. Must complete their onboarding first.

### 5.5 Salary Details and Revision (UF-15, UF-21, UF-22)

**Arjun Mehta — Salary Detail:**

| Component | Monthly | Annual | % of CTC |
|-----------|---------|--------|----------|
| Basic | ₹40,000 | ₹4,80,000 | 57.14% |
| HRA | ₹16,000 | ₹1,92,000 | 22.86% |
| Fixed Allowance | ₹14,000 | ₹1,68,000 | 20.00% |
| Total CTC | ₹70,000 | ₹8,40,000 | 100% |

**Pending revision (awaiting approval):** ₹8,40,000 → ₹9,45,000/year (13% increase), effective June 2026. Component ratios preserved.

**Revision form fields:**

| Field | Type | Notes |
|-------|------|-------|
| Salary Template | Dropdown | Select structure |
| Revision Type | Radio | By % or New Amount |
| Revised Annual CTC | Currency | New annual figure |
| Basic % | Number | Can modify ratio |
| HRA % | Number | Can modify ratio |
| Fixed Allowance | Read-only | Auto-computed residual |
| Effective From | Month picker | Calendar blocks past months; text input accepts them |
| Payout Month | Month picker | When arrears (if any) are paid |

**Arrears auto-calculation:** Zoho automatically calculates arrears if effective date is backdated before current month. Note text: "Zoho Payroll will automatically calculate any arrears."

### 5.6 Statutory Settings Per Employee (UF-16)

Accessed via: Employee profile → Overview tab → Pencil icon on statutory section

| Setting | Per-Employee Toggle | Default |
|---------|--------------------|----|
| EPF | Enable/Disable | Depends on org config |
| ESI | Enable/Disable | Depends on org config |
| PT | Enable/Disable | Depends on org config |
| LWF | Enable/Disable | Depends on org config |

**No effective date** for statutory changes — applies immediately. No audit trail visible in UI.

### 5.7 Bank Details (UF-24)

**Payment modes:**
- Direct Deposit (Automated) — via Zoho Payments / ICICI / HSBC integration
- Bank Transfer (Manual) — admin uploads bank advice to bank
- Cheque
- Cash

**Account Number:** Displayed masked (XXXX7890). Edit requires separate "Change" button — not inline edit.

**IFSC:** Real-time verification against bank database.

**No approval workflow for bank changes** — admin can change bank details unilaterally. Risk: salary misdirection.

### 5.8 Employee Documents (UF-17)

**Critical gap:** There is NO Documents tab on individual employee profiles. Documents is a global sidebar module (`#/documents/folder`), not per-employee.

**Global Documents module:**
- Org Folder: Offer Letters
- Employee Folder: Personal Documents
- Storage: 1GB / 100 employees
- Upload: Drag-and-drop, max 50MB per file
- ZIP upload: Files inside must have employee ID as filename for auto-assignment

### 5.9 Bulk Import (UF-20)

14 import types in 5 groups:

| Group | Import Types |
|-------|-------------|
| Employee Details | Employee Personal Details, Employee Employment Details, Employee Bank Details |
| Salary Details | Salary Details, Salary Revision, Salary Arrears, Variable Pay, Loan Details, Prior Payroll |
| Complete Employee Basic | Complete Employee Basic Details (all-in-one) |
| Employee Exit | Employee Exit Details |
| Investments | Tax Declaration, Investment Declaration, Previous Employment Details, Tax Regime Details |

Sample CSV: 25 columns. Prior Payroll import locked after first pay run is processed.

---

## 6. IT Declaration and TDS

### 6.1 IT Declaration Lifecycle

**Admin controls (UF-26, UF-28):**

| Action | URL | Effect |
|--------|-----|--------|
| Release IT Declaration | `#/settings/preferences/it-declaration` | Unlocks for employees |
| Lock IT Declaration | Same page | Blocks further employee submissions |

**Current demo state:** IT Declaration is LOCKED. This is the root cause of all TDS = ₹0 in May 2026 pay run.

**Settings on this page:**

| Setting | State |
|---------|-------|
| Allow employees to switch tax regimes | CHECKED (enabled) |
| Allow TDS modification to exceed calculated tax | NOT checked |
| Process payroll with approved POI amount from | March (default) |

**Important:** "Allow employees to switch tax regimes" is CHECKED — employees can switch between new and old regime via the portal. This contradicts the V1 build intent of "new regime only" and means old regime TDS logic is exposed in Zoho Payroll.

### 6.2 IT Declaration Form (UF-27)

**Employee-facing (via portal). Multi-step:**

- Page 1: Investment declarations (HRA, 80C, 80D, 80G, Other Sources, House Property)
- Page 2: Tax regime selection (New vs Old)
- "Submit and Compare" button — allows employee to see tax difference between regimes

**Section inventory in demo org (UF-25, UF-27):**

| Section | Sub-fields | Regime Applicability |
|---------|-----------|---------------------|
| House Property | Rental income, HRA details | Old regime (80C HRA exemption) |
| Other Sources of Income | Interest income, dividend, etc. | Both |
| Section 123 (80C) | LIC, EPF, PPF, ELSS, NSC, etc. — ₹1.5L limit | Old regime only |
| Section 126 (80D) | Medical insurance — ₹1L limit | Old regime only |
| Other Investments | NPS 80CCD(1B) — ₹50,000 | Allowed in new regime |

**URL anomaly:** Admin submission URL contains `?tax_regime=with_exemptions` — suggesting old regime is the system default even for new-regime orgs. This is a potential configuration or labeling bug.

### 6.3 POI (Proof of Investment) Workflow (UF-29, UF-30, UF-31, UF-90)

**Pre-conditions:** IT Declaration must be released AND employee has declared investment amounts.

**Settings for POI window (UF-29):**

| Setting | Description |
|---------|-------------|
| Process payroll with approved POI amount from | Month from which approved POI affects TDS (default: March) |
| Allow regime switch | Whether employee can switch during POI phase |
| Allow TDS modification during Payroll | Admin can adjust TDS amount |
| Mandate attachment | Require document upload |
| Mandate reviewer comments | Require admin to add comment when approving/rejecting |

**POI Approvals page (`#/approvals/proof-of-investment`):**
- Filters: Fiscal Year, Tax Regime, Employee
- "2 employee(s) yet to submit POI" count visible (info bar)
- Empty state: No POI submitted (IT Declaration locked)

**Admin approval actions:** Approve full amount OR reduce amount (partial approval). Approved amount feeds TDS computation. Rejected POI → investment unverified → TDS increases.

### 6.4 TDS Computation (UF-32, UF-33, UF-40)

**New Regime FY2026-27 Tax Slabs:**

| Income Range | Rate |
|-------------|------|
| Up to ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| Above ₹24,00,000 | 30% |

**Section 87A rebate:** If taxable income ≤ ₹7,00,000, tax = ₹0 (full rebate).

**Standard Deduction (New Regime):** ₹75,000 (increased from ₹50,000 in FY2024-25).

**Arjun's Expected TDS Calculation:**

| Item | Amount |
|------|--------|
| Annual CTC | ₹8,40,000 |
| Standard Deduction | −₹75,000 |
| Net Taxable Income | ₹7,65,000 |
| Tax on ₹7,65,000 (new regime) | 0% on ₹4L = ₹0; 5% on ₹3,65,000 = ₹18,250 |
| 4% Cess on ₹18,250 | ₹730 |
| Total Annual Tax | ₹18,980 |
| Monthly TDS | ₹1,582 |

**Actual TDS in May 2026:** ₹0 (because IT Declaration is locked — system not computing TDS).

**Priya's TDS:** ₹22,000/month × 12 = ₹2,64,000 annual − ₹75,000 standard deduction = ₹1,89,000 taxable. Below ₹4L slab → TDS = ₹0 (correct).

### 6.5 TDS Liabilities (UF-68)

**URL:** `#/taxes-and-forms/tax-liabilities/pending`

- Tabs: Unpaid | Paid
- Filters: Quarter, Month
- TDS deposit due: 7th of following month (except March → 30th April)
- TDS liability record created after pay run finalization

**TDS Challans (UF-89):**

**URL:** `#/taxes-and-forms/tax-payments/unassociated`

- Tabs: Unassociated | Associated
- Challan form fields: TAN (auto), ITNS 281, BSR Code (7 digits), Challan Date, Serial Number, Amount, Nature of Payment (Section 192), Month
- Association: Links challan to specific month's TDS liability → required before Form 24Q generation

---

## 7. Employee Exit and Gratuity

### 7.1 Full and Final Settlement (UF-34)

**Entry point:** Employee profile → "Mark as Resigned" or similar action (not directly observed — no exited employees in demo).

**FnF pay run components:**

| Component | Calculation |
|-----------|------------|
| Unpaid salary | Days worked in last month × daily rate |
| Notice period pay | If employer waives notice: pay the period |
| Notice period recovery | If employee doesn't serve notice: deduct |
| Leave encashment | Earned leave balance × daily rate |
| Gratuity | If service ≥ 5 years |
| Bonus | Any pending bonus |
| Variable pay | Any due performance pay |
| Loan recovery | Outstanding loan balance deducted |
| TDS | On FnF amount (spread/projected) |

FnF is processed as a special pay run type. Employee remains in the system post-exit (soft delete).

### 7.2 Gratuity Computation (UF-35)

**Formula:** (Monthly Basic × 15 / 26) × Years of Completed Service

**Statutory limits:**
- Eligibility: Minimum 5 years of continuous service
- Exemption: Up to ₹20,00,000 exempt from income tax (Gratuity Act, 1972)
- DA: Usually absent in private sector — gratuity on Basic only

**Zoho behavior:** Does NOT auto-calculate gratuity. Admin manually enters gratuity amount as a Variable pay component in the FnF pay run.

**Component configuration:** Variable, Flat Amount; EPF = No; ESI = No.

**Gratuity Calculation Date:** Can differ from DOJ (configurable during bulk import). Used when employee has prior service recognized by employer.

---

## 8. Pay Runs — Regular Cycle

### 8.1 Pay Run State Machine (UF-46)

```
Draft → Under Review → Approved → Paid (Finalized)
```

| State | Description | Actions Available |
|-------|-------------|------------------|
| Draft | In progress; editable | Edit LOP, Add Variable Pay, Add Reimbursements |
| Under Review | Submitted for approval | Approvers can approve/reject |
| Approved | All approvers signed off | "Record Payment" to finalize |
| Paid | Finalized; immutable | Download Payslips, Download TDS, Delete Recorded Payment |

### 8.2 May 2026 Pay Run Data (UF-36)

| Field | Value |
|-------|-------|
| Pay Run ID | 3848927000000034159 |
| Period | 01/05/2026 – 31/05/2026 |
| Base Days | 31 |
| Pay Date | 29/05/2026 |
| Total Net Pay (Payroll Cost) | ₹87,484 |
| Employees Paid | 2 (Arjun, Priya) |
| Employees Skipped | 3 (Vikram, Aisha, Rahul?) |

**Pay Run Tabs:**
1. Employee Summary — employee-wise pay breakdown
2. Taxes & Deductions — EPF, ESI, PT, TDS, Loans columns
3. Overall Insights — Aggregate stats, Statutory Summary, Component-Wise Breakdown

**Overall Insights drill-down:** Clickable links in Component-Wise Breakdown navigate to individual employee rows.

### 8.3 Individual Employee Pay Data — May 2026 (UF-50)

**Arjun Mehta:**

| Item | Value |
|------|-------|
| Payable Days | 31 |
| LOP Days | 2 |
| Actual Paid Days | 29 |
| Basic (prorated 29/31) | ₹37,417 |
| HRA (prorated 29/31) | ₹14,967 |
| Fixed Allowance | ₹13,100 (NOT prorated — anomaly) |
| Total Earnings | ₹65,484 |
| All Deductions | ₹0 |
| Net Pay | ₹65,484 |

**Priya Sharma:**

| Item | Value |
|------|-------|
| Payable Days | 31 |
| LOP Days | 0 |
| Actual Paid Days | 31 |
| Monthly Salary | ₹22,000 |
| Net Pay | ₹22,000 |

### 8.4 LOP (Loss of Pay) Calculation (UF-42, UF-45)

**Formula:** Component Monthly Amount × (Payable Days / Calendar Days in Month)

**For Arjun (LOP 2 days, May = 31 calendar days, Payable = 29 days):**

| Component | Full Month | Prorated (29/31) | Notes |
|-----------|-----------|-----------------|-------|
| Basic | ₹40,000 | ₹37,417 | Prorated |
| HRA | ₹16,000 | ₹14,967 | Prorated |
| Fixed Allowance | ₹14,000 | ₹13,100 | Appears NOT prorated (anomaly) |

**Proration basis:** Calendar days (not working days). Mid-month joiner joining on 16th May: proration = 16/31. Confirmed from UF-48.

**LOP impact on statutory deductions:** Reduced gross affects PT slab, ESI wage, PF wage, TDS computation.

### 8.5 Statutory Deductions Summary — May 2026 (UF-37–UF-40)

| Deduction | Arjun | Priya | Reason for ₹0 |
|-----------|-------|-------|--------------|
| EPF (EE) | ₹0 | ₹0 | EPF Disabled for all employees |
| ESI (EE) | ₹0 | ₹0 | All employees above ₹21,000 ceiling |
| PT | ₹0 | ₹0 | Kerala PT not due in May (half-yearly: Sep and Mar) |
| TDS | ₹0 | ₹0 | IT Declaration locked; TDS not computing |
| Loan EMI | ₹0 | ₹0 | Arjun's EMI starts July 2026 |

### 8.6 Mark as Paid — Record Payment (UF-47)

**Modal fields:**

| Field | Type | Required |
|-------|------|----------|
| Payment Date | Date (dd/MM/yyyy) | Yes |
| Payment Mode | Dropdown (NEFT/RTGS/Cash/Cheque) | Yes |
| Remarks | Text | No |

**Post-payment:** Pay run → PAID state. Bank Advice downloadable. Payslips published to employee portal. Zoho does NOT initiate actual bank transfer — only generates the Bank Advice file.

### 8.7 Pay Run Header Dropdown (PAID state) (UF-50)

| Action | Description |
|--------|-------------|
| Download all Payslips | ZIP of all employee payslips |
| Download all TDS Worksheets | ZIP of TDS computation sheets |
| Show Downloads | View previously generated downloads |
| Delete Recorded Payment | Reverts PAID state (reversal) |

### 8.8 Bank Advice (UF-51)

Expected columns: Employee Name, ID, Account Number, IFSC, Bank Name, Account Type, Net Pay, Reference, Remarks.

**May 2026 totals:** Arjun ₹65,484 + Priya ₹22,000 = ₹87,484.

Format: Excel/CSV (exact format not confirmed — button not clicked to avoid disruption).

### 8.9 Payslip Format (UF-50)

Payslip opens as a slide-in panel, not a separate page.

**Payslip panel structure (Arjun, May 2026):**
- Header: Employee name, EMP ID, Pay Period, Net Pay
- Payable Days, LOP Days, Actual Paid Days
- Earnings table: Component | Amount
- Deductions table: Component | Amount
- Net Pay summary
- Download PDF button

**7 payslip templates available** (Settings > PDF Templates): Elegant (default), Standard, Mini, Simple, Lite, Simple Spreadsheet, Professional.

**Separate templates for FnF payslip.** Letter templates: Salary Certificate, Salary Revision Letter, Bonus Letter.

### 8.10 New Joiner Proration (UF-48)

**Calculation:** Component × (Days Present / Calendar Days in Month)

**Example:** Employee joins 16th May 2026:
- Days present: 16 (May 16–31)
- Calendar days: 31
- Proration factor: 16/31 = 0.5161
- Basic ₹40,000 × 16/31 = ₹20,645

---

## 9. Pay Runs — Special Types

### 9.1 Creating a Special Pay Run (UF-42, UF-52)

From Pay Runs list page, "New" dropdown shows:
- One Time Payout
- Off Cycle Payrun

Regular monthly pay run is automatically created at month start — not manually triggered via "New."

### 9.2 Off-Cycle Pay Run (UF-52)

**Entry:** Pay Runs → New → Off Cycle Payrun → Modal with single field: Pay Date.

No Reason field. No minimum notice period. Creates a new pay run for a non-standard period. Admin then adds employees and amounts.

### 9.3 Bonus Pay Run (UF-53)

Two mechanisms:
1. Bonus component in regular pay run (via Variable Input) — small ad-hoc bonuses
2. Standalone off-cycle bonus run — large annual bonuses (Diwali, performance)

**TDS on bonus:** Spreading method — bonus income added to annual projection, revised annual tax computed, additional TDS = revised total − already deducted. Deducted in the bonus pay run month.

**Payment of Bonus Act, 1965:** Applies to 20+ employee establishments. Eligible employees: ≤ ₹21,000/month. Minimum bonus: 8.33% of annual wages (or ₹7,000 × months, whichever higher). Maximum: 20%. Due within 8 months of year-end.

### 9.4 Arrears Pay Run (UF-54)

**Trigger:** Salary revision with backdated effective date. Zoho may auto-prompt: "Arrears pending for X months. Create arrears run?"

**Scenario (Arjun's pending revision):**
- Old salary: ₹70,000/month
- New salary: ₹78,750/month
- Effective from: April 2026 (hypothetical)
- Arrears for April + May: (₹78,750 − ₹70,000) × 2 = ₹17,500

**TDS on arrears:** Taxable in year of receipt (Section 192). Spread across remaining FY months for TDS computation.

**Section 89(1) relief:** Employee can claim via Form 10E (Income Tax Portal) if arrears relate to prior FY. Reduces TDS on arrears. Zoho shows TDS without 89(1) relief — employee claims at ITR filing.

**PF/ESI on arrears:** NOT computed on arrears amounts (only on regular salary).

### 9.5 One-Time Payout (UF-55)

First option in "New" dropdown. For ad-hoc payments: Performance Bonus (taxable), Gift (>₹5,000 taxable), ex-gratia (taxable).

### 9.6 Past Pay Runs — Historical View (UF-56)

Past pay runs are read-only after finalization. Available artifacts:

| Artifact | Format |
|---------|--------|
| Payslip (per employee) | PDF |
| All Payslips (bulk) | ZIP |
| TDS Worksheet | PDF/Excel |
| Bank Advice | Excel/CSV |

**What is immutable post-finalization:** Salaries, LOP, Variable Pay, Payment Date, Payslip content, TDS amounts, Employee list.

**Correction mechanism:** Revision Pay Run only — no direct edit.

### 9.7 Revision Pay Run (UF-57)

Supplementary run for the SAME pay period, containing only delta corrections.

**When to use:**
- Employee paid wrong salary
- Wrong LOP days (over/under)
- Missed variable input (bonus not paid)
- PF/ESI computed incorrectly

**Not a revision scenario:** Wrong bank account (bank-side recall) or employee exit mid-month (use FnF instead).

**Impact:**
- TDS liability records updated for prior month
- ECR must be re-generated and re-submitted to EPFO if PF amounts change
- Whether revised payslip replaces or supplements the original — not confirmed

### 9.8 Pay Run Reversal (UF-58)

**Mechanism:** "Delete Recorded Payment" in PAID pay run header dropdown.

**Effect:** Reverts PAID → editable state. Payment date cleared. Payslips may be unpublished.

**What it does NOT do:** Does not reverse actual bank transfers. Does not revoke statutory challans already paid (PF, ESI, TDS).

**Risk table:**

| Risk | Impact |
|------|--------|
| Employees already downloaded payslip | Outdated payslip in employee possession |
| Salary already credited to bank | Cannot be recalled via Zoho |
| TDS liability reverted | Form 24Q data temporarily incorrect |
| PF challan already paid | Adjust in next month's ECR |
| ESI challan already paid | Adjust in next period via ESIC |

**Best practice:** Use reversal only before bank transfer is initiated (same day as finalization).

---

## 10. Approvals Module

### 10.1 Approvals Sub-Modules (UF-59)

**URL pattern:** `#/approvals/{sub-module}`

| Sub-module | URL | Purpose |
|-----------|-----|---------|
| Reimbursements | `#/approvals/reimbursements` | Approve employee expense claims |
| Salary Revision | `#/approvals/salary-revision` | Approve salary change requests |
| Proof of Investments | `#/approvals/proof-of-investment` | Verify POI documents |

All three showed empty state in demo org (no active items, IT Declaration locked).

### 10.2 Reimbursement Approval (UF-60)

**Flow (when active):**
1. Employee submits claim via portal
2. Claim appears in Approvals > Reimbursements
3. Admin reviews: approve full / approve partial / reject
4. On approval: select payout month
5. Claim included in that month's pay run
6. Employee receives reimbursement in salary

**Filters:** Claim Month, Payout Month, Employee.

### 10.3 POI Approval (UF-90)

**Header elements:**
- "All Investments" dropdown — filter by investment category
- "2 employee(s) yet to submit POI" count (info bar)

**Per-submission review fields:** Employee Name, Investment Category, Declared Amount, Uploaded Document, Approved Amount (editable), Approve/Reject actions.

### 10.4 Approval History (UF-62)

No dedicated approval history view visible in the Approvals module. The Activity Logs report (Reports Centre) is the primary audit trail for approval actions.

---

## 11. Loans Module

### 11.1 Loan List (UF-63)

**URL:** `#/loans`

**Columns:** Employee, Loan Number, Loan Name, Status, Loan Amount, Amount Repaid, Remaining Amount.

**Demo org loans:**

| Loan # | Employee | Type | Amount | Status | EMI | First EMI | Perquisite |
|--------|----------|------|--------|--------|-----|----------|-----------|
| LOAN-00001 | Arjun Mehta | Personal Loan | ₹50,000 | Open | ₹5,000 × 10 | 01/07/2026 | Exempt (₹50k < ₹2L threshold) |
| LOAN-00002 | Vikram Nair | Emergency Loan | ₹1,00,000 | Open | Not confirmed | Not confirmed | Unknown |

### 11.2 Create Loan Form (UF-63)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Loan Name | Dropdown | Yes | From configured loan types (Personal, Emergency, etc.) |
| Employee | Combobox | Yes | Searchable |
| Loan Amount | Currency (₹) | Yes | |
| Number of Instalments | Number | Yes | |
| Instalment Amount | Currency | Auto-computed | Loan Amount / Instalments |
| Disbursement Date | Date | Yes | When loan is disbursed |
| First Instalment Date | Date | Yes | When first EMI deducts |
| Reason | Text | No | |
| Exempt from perquisite | Checkbox | No | Rule 15(5): loans < ₹2L aggregate exempt |

**No instalment preview** before saving. UI label says "IT Rules, 2026" for perquisite exemption — likely errata for "1962".

### 11.3 Loan Detail Panel (UF-65)

Slide-in drawer with:
- Header: Loan ID, "Open" status, "Record Repayment" button, three-dot menu
- Tabs: Details, Repayment Schedule
- Repayment Schedule: Empty until first EMI paid

**Loan actions (three-dot menu):**
- Edit Loan
- Pause Instalment Deduction
- Delete Loan

### 11.4 Loan EMI in Pay Run (UF-64)

EMI appears under "Benefits" section in Taxes & Deductions tab. First EMI deducts in the pay run covering the First Instalment Date (Arjun: July 2026).

**Vikram's loan:** Vikram is skipped from pay runs (onboarding incomplete). His loan EMI will never deduct until onboarding is completed — perpetual outstanding balance.

### 11.5 Loan Perquisite (UF-66)

**Formula:** Outstanding Balance × SBI MCLR / 12

**Exemptions (Rule 15(5)):**
- Medical loans under Rule 18: fully exempt
- Aggregate outstanding < ₹2,00,000: fully exempt

**Arjun:** ₹50,000 < ₹2L threshold → exempt → Perquisite = ₹0.

**Vikram:** ₹1,00,000 alone < ₹2L. But if aggregate of all loans exceeds ₹2L, perquisite applies on the entire outstanding.

**SBI MCLR rate configuration:** Not found in Settings. Unclear if Zoho maintains this or requires admin to update manually.

### 11.6 Foreclosure (UF-67)

No explicit "Foreclose" button. Admin uses "Record Repayment" with full outstanding amount as the repayment amount → loan auto-closes.

**Delete Loan:** Should only be allowed before first EMI. Closed loans retained for audit trail.

### 11.7 Loan Reports (UF-80)

| Report | Purpose |
|--------|---------|
| Loan Outstanding Summary | Point-in-time outstanding per loan |
| Loan Perquisite Summary | Monthly perquisite value per loan |
| Loan Perquisite Projection | Future month-by-month perquisite forecast |
| Loan Summary Report | Comprehensive view: current and historical loans |

---

## 12. Taxes and Statutory Forms

### 12.1 Taxes & Forms Navigation (UF-89)

**URL:** `#/taxes-and-forms/...`

| Sub-item | URL | Purpose |
|----------|-----|---------|
| TDS Liabilities | `/tax-liabilities/pending` | Monthly TDS tracking |
| Challans | `/tax-payments/unassociated` | Record ITNS 281 challan payments |
| Form 24Q | `/form24q` | Quarterly TDS return |
| Form 16 | `/form16` | Annual TDS certificate |

**EPF, ESI, PT, LWF are NOT in this sidebar** — accessed only via Reports > Statutory Reports.

### 12.2 EPF ECR Generation (UF-69)

**ECR = Electronic Challan cum Return.** Text file with `#` delimiter.

**ECR file fields per employee:**
UAN, Member Name, Gross Wages, EPF Wages, EPS Wages, EDLI Wages, EE EPF, ER EPF, ER EPS, NCP Days, Refund of Advances.

**Submission:** Via EPFO Unified Portal (epfindia.gov.in).
**Due date:** 15th of the following month.

**Demo org:** All EPF = ₹0 (EPF Disabled for all employees). EPF number has Karnataka prefix but org is in Kerala — flagged.

### 12.3 ESI Return and Challan (UF-70)

**ESI rates:** EE 0.75%, ER 3.25% of gross salary.
**Wage ceiling:** ₹21,000/month.
**Contribution periods:** April–September and October–March.
**Challan due date:** 21st of the following month.
**Form 5 (half-yearly return):** Due 11th November and 12th May.

**IP Number:** Insurance Person Number — assigned by ESIC to each covered employee.

### 12.4 PT Challan (UF-71)

**Kerala PT:** Half-yearly. Challan due in September and March.
**PT Number:** Not configured — challan generation blocked.

See Section 3.3 for PT slab table.

### 12.5 LWF Challan (UF-72)

**Kerala LWF:** Monthly. ₹50 EE + ₹50 ER.
**LWF Registration Number:** Not visible in Settings — unclear if required.

### 12.6 Form 24Q — TDS Return (UF-73)

**URL:** `#/taxes-and-forms/form24q`

**Quarterly schedule for FY2026-27:**

| Quarter | Period | Due Date |
|---------|--------|---------|
| Q1 | April – June 2026 | 31/07/2026 |
| Q2 | July – September 2026 | 31/10/2026 |
| Q3 | October – December 2026 | 31/01/2027 |
| Q4 | January – March 2027 | 31/05/2027 |

**Pre-conditions for generation:** TAN configured, Deductor Name selected, challans associated to liabilities.

**"Generate Text File"** navigates to preferences URL. Text file uploaded to TRACES/NSDL. BSR code + challan serial required.

**Demo org:** All monthly TDS = ₹0 (IT Declaration locked). Q1 due date: 31/07/2026.

### 12.7 Form 16 (UF-74, UF-75, UF-76)

**4-step process:**

| Step | Action | Status in Demo |
|------|--------|---------------|
| 1 | Upload Part A (from TRACES) | Blocked — "Tax Deductor not found" |
| 2 | Generate Part B | Disabled (requires Step 1) |
| 3 | Sign (DSC or manual) | Disabled |
| 4 | Publish/Email to employees | Disabled |

**Part A:** Generated by TRACES; employer downloads and uploads to Zoho.
**Part B content:** Employer Details, Employee Details, Gross Salary, Exemptions u/s 10, Standard Deduction ₹75,000, Net Salary, Chapter VI-A deductions, Taxable Income, Tax computation, TDS reconciliation.

**Root cause of blockage:** "Tax Deductor not found" refers to the Deductor's Name field in Settings > Tax Details (not the TAN). TAN is configured (MUMR12345A) but the responsible person (Finance Manager/admin) is not selected.

**Issue deadline:** 15th June.

---

## 13. Reports Centre

### 13.1 Overview (UF-77)

**URL:** `#/reports`

**39 system reports across 9 categories:**

| Category | Report Count |
|----------|-------------|
| Payroll Overview | 9 |
| Statutory Reports | 8 |
| Employee / Contractor | 7 |
| Declarations & Investments | 3 |
| Deduction Reports | 4 |
| Taxes and Forms | 2 |
| Loan Reports | 4 |
| Payroll Journal | 1 |
| Activity | 1 |

**No custom report builder.** Standard filters: Employee, Department, Pay Period, Date Range. Export formats: Excel, CSV, PDF, Email.

### 13.2 Statutory Reports (UF-79)

| Report | Content |
|--------|---------|
| EPF Summary | Monthly EPF contributions per employee |
| EPF ECR | EPFO text file download |
| ESI Summary | Monthly ESI contributions per employee |
| ESI Monthly | Detailed ESI data |
| PT Summary | PT deductions per employee |
| Employee-wise PT | Individual PT detail |
| Annual PT | Full-year PT data |
| LWF Summary | LWF contributions |

**Statutory Filing Calendar (May 2026):**

| Statutory | Filing | Due Date |
|-----------|--------|---------|
| EPF ECR | May 2026 | 15/06/2026 |
| ESI Challan | May 2026 | 21/06/2026 |
| TDS (ITNS 281) | May 2026 | 07/06/2026 |
| PT Challan | N/A (Kerala — half-yearly) | September 2026 |

### 13.3 Employee and Contractor Reports (UF-81)

| Report | Key Content |
|--------|------------|
| Compensation Details | CTC and component breakup per employee |
| Reimbursement Claim Summary | All claims across status |
| Employee Perquisites Summary | Perquisite values per employee per month |
| Full and Final Settlement Report | FnF computation for exited employees |
| Employees' Salary Revisions | Current FY revisions |
| Salary Revision History | Complete historical revisions per employee |
| Salary Withhold Report | Withheld salaries with reason and release date |

### 13.4 Declarations and Deduction Reports (UF-82)

| Report | Notes |
|--------|-------|
| FBP Declaration Report | FBP allocation per employee; empty (no FBP in demo) |
| Investment Declaration Report | IT declarations; all ₹0 (locked) |
| Proof of Investment Report | POI submission status; empty |
| Benefits & Deductions Summary | Combined earnings and deductions view |
| Deductions Summary | All deductions per employee per month |
| Benefits Summary | All earnings per employee per month |
| Donations Summary | 80G claims — irrelevant in new regime |

### 13.5 Payroll Journal (UF-83)

Accounting journal entries per pay run. May 2026 (all deductions = ₹0):
- Dr: Salaries & Wages ₹87,484
- Cr: Bank ₹87,484

**Integration:** Admin downloads journal as CSV and imports into Tally/Zoho Books/SAP. Zoho Books direct integration available (not connected in demo).

**GL account mapping:** Admin maps salary components to GL codes. If not configured, defaults to single "Salaries & Wages" account.

### 13.6 Activity Logs (UF-83)

System-wide audit trail of all user actions.

**Expected columns:** Date/Time, User, Action, Module, Entity, Entity ID, Old Value, New Value, IP Address.

**Action categories:** Employee CRUD, Pay Run lifecycle, Salary changes, Loans, Declarations, Settings changes, User Management, Login/Logout.

**Filters:** Date Range, User, Module, Action Type.
**Export:** Excel/CSV.
**Tamper-evident:** No delete option for activity logs (admin cannot purge audit trail).

---

## 14. Employee Portal

### 14.1 Portal Overview (UF-84, UF-85)

**Mobile app:**
- iOS: "Employee Portal - Zoho Payroll" (App ID: 1450810850) — Apple App Store
- Android: `com.zoho.payroll` — Google Play Store

**Web portal:** Likely `payroll.zoho.in` with employee credentials. Exact URL not confirmed.

**Invitation flow:** Admin invites employee via email → employee creates Zoho account → portal access.

### 14.2 Portal Settings (UF-84)

**URL:** `#/settings/portal/preferences`

| Setting | State |
|---------|-------|
| Enable Portal Access | Active (enabled) |
| Banner Message | Empty |
| Banner Display Until | Empty |
| Portal Contact Email | abhijithss2255@gmail.com |
| Show documents in employee portal | Unchecked |

**Web Tabs** (`#/settings/portal/webtabs`): Empty — no custom external URLs configured.

### 14.3 Employee Portal Features

| Feature | Admin Precondition | Employee Action |
|---------|-------------------|----------------|
| View payslips | None | Download PDF |
| View TDS worksheet | None | View/download |
| IT Declaration | Admin must Release | Submit investments |
| FBP Declaration | FBP component must be active | Allocate amounts |
| POI upload | IT Declaration released + POI window open | Upload documents |
| Reimbursement claim | Components must be active | Submit with receipt |
| Form 16 | Admin must Publish | Download PDF |
| Document access | "Show documents" checkbox must be enabled | View/download |

### 14.4 Reimbursement Claim Flow (UF-88)

**Pre-condition:** At least one reimbursement component must be ACTIVE with non-zero maximum.

**Claim form fields:** Reimbursement Type, Claim Amount, Claim Date, Description, Bill/Receipt Upload (PDF/JPG, max 5MB).

**Status flow:** Submitted → Pending → Approved/Rejected → Paid (included in pay run).

**Admin can approve partial amount.** Rejected claim requires fresh submission (no edit-and-resubmit).

**New Regime impact:** Most reimbursement exemptions not available — employee receives reimbursement as fully taxable income.

**LTA:** 2 journeys per 4-year block. Economy fare only. Actual travel required.

---

## 15. Additional Settings

### 15.1 Tax Details (UF-95)

**URL:** `#/settings/taxes`

| Field | Value | Notes |
|-------|-------|-------|
| Organisation PAN | ABCDE1234F | Test PAN — invalid in production |
| TAN | MUMR12345A | Configured; MUMR = Mumbai Range (mismatch with Kerala) |
| TDS Circle / AO Code | Blank | Required for Form 24Q — currently missing |
| Tax Payment Frequency | Monthly | Not editable |
| Deductor's Type | Employee | Responsible person is an org employee |
| Deductor's Name | Not selected | Root cause of "Tax Deductor not found" error |

**Without Deductor Name:** Form 24Q signature field empty → return may be rejected by TRACES.

### 15.2 Users and Roles (UF-91)

**URL:** `#/settings/users-roles/users` and `/roles`

**Current users:** 1 (abhijithss2255@gmail.com, Admin role, Active).

**System-defined roles (3):**

| Role | Access |
|------|--------|
| Admin | Unrestricted — all modules including org settings |
| Manager | All modules except Organisation Settings |
| Reimbursements and POI Reviewer | Only Approvals > Reimbursements and POI |

**Custom roles:** "New Role" button — admin defines per-module permissions.

**RBAC model:** Role-based. Each user has exactly one role. No per-employee data restrictions.

**User invite flow:** "Invite User" → email → select role → send. Invited status until accepted.

### 15.3 Direct Deposits and Integrations (UF-92)

**URL:** `#/settings/direct-deposit`

| Integration | Availability | Cost |
|-------------|-------------|------|
| Zoho Payments Payout | Available | ₹3/employee/pay run + 18% GST = ₹3.54 effective |
| ICICI Bank | Locked (trial plan) | Requires paid plan |
| HSBC Bank | Available | No trial restriction mentioned |

**Zoho App Integrations** (`#/settings/integrations/zoho`):

| Integration | Purpose | Status |
|-------------|---------|--------|
| Zoho People | Employee master + LOP sync | Not connected |
| Zoho Books | Auto-create payroll journal entries | Not connected |
| Zoho Expense | Employee expense reimbursements | Not connected |
| Zoho Analytics (Beta) | Custom reports dashboard | Not connected |

**Zoho People integration benefit:** Eliminates manual LOP entry — attendance data auto-populates payroll.

### 15.4 PDF and Email Templates (UF-93)

**PDF Templates** (`#/settings/templates/regular-payslip`):
- 7 payslip templates: Elegant (default), Standard, Mini, Simple, Lite, Simple Spreadsheet, Professional
- Separate FnF payslip template
- Letter templates: Salary Certificate, Salary Revision Letter, Bonus Letter

**Email Templates** (`#/settings/email-templates`):
- Payslip Notification (portal-enabled employees) — email with portal link
- Payslip Notification (portal-disabled employees) — email with PDF attachment
- Off-Cycle and One-Time Payroll Notification
- Full and Final Settlement Notification

**Sender Email:** Configurable at `#/settings/email-preference`. Custom sender domain possible.

### 15.5 Loans Settings (UF-94)

**URL:** `#/settings/loan/custom-field/list`

Sub-tabs: Custom Field, Custom Button, Validation Rules, Record Locking, Related List.

**Custom fields:** 0 of 59 used. Types: Text, Number, Decimal, Date, Dropdown, Checkbox, User, URL, File Upload.

**Loan Types (Personal, Emergency, etc.):** Configured in the Loans module directly, NOT in Settings.

### 15.6 Approval Workflows — Settings (UF-96)

Pay Run approval types (radio group — same three types as Salary Revision):
- Simple Approval (default)
- Multi-Level Approval
- Custom Approval (criteria-based)

**Additional pay run settings tabs:** Custom Button, Record Locking, Related List.

**Business rule:** Changing approval workflow takes effect for future pay runs only.

### 15.7 Giving Module (UF-84)

**URL:** `#/donations`

Charity campaign management. Admin creates campaigns; employees contribute through payroll. 80G deduction for contributions. Empty state in demo org.

### 15.8 Leave and Attendance Settings (UF-96)

**URL:** `#/settings/holiday-leave/enable-module`

Separate module integrating with Zoho People. Leave types, holiday list, LOP calculation method. Demo org: Leave module likely disabled (no Zoho People integration).

---

## 16. Design System

### 16.1 Application Shell (DS-01)

```
┌────────────────────────────────────────────────────────┐
│                     Top Band (Header)                   │
├─────────┬──────────────────────────────────────────────┤
│  Left   │         Main Content Area                    │
│ Sidebar │   [Breadcrumb / Page Title]                  │
│  Nav    │   [Action Buttons]                           │
│         │   [Tab Navigation]                           │
│         │   [Content: Tables / Cards / Forms]          │
│         │   [Detail Drawer (slides in right)]         │
└─────────┴──────────────────────────────────────────────┘
```

**Zoho design system:** Zoho Catalyst (proprietary). Not MUI, Tailwind, or Ant Design.

### 16.2 Top Band Components (DS-01)

| Component | Notes |
|-----------|-------|
| Logo + "Payroll" label | Clicks to Dashboard |
| Global Employee Search | With Advanced Search |
| Trial Banner | "Expires in N day(s)" + Upgrade button |
| Organization Switcher | Multi-org support ("lerno" shown) |
| Refer and Earn | Referral program |
| Notifications Bell | Badge count |
| Settings Gear | Opens Settings overlay |
| User Avatar | Account menu |
| Zoho Cliq (float) | Bottom-right chat integration |

### 16.3 Left Sidebar Navigation (DS-01)

| Item | URL | Expandable |
|------|-----|-----------|
| Dashboard | `#/home/dashboard` | No |
| Employees | `#/people/employees` | No |
| Pay Runs | `#/payruns` | No |
| Approvals | — | Yes (3 sub-items) |
| Taxes & Forms | — | Yes (4 sub-items) |
| Loans | `#/loans` | No |
| Giving | `#/donations` | No |
| [Unknown item] | — | Unknown |
| Documents | `#/documents/folder` | No |
| Reports | `#/reports` | No |
| Settings | `#/settings` | Opens overlay |
| Contact Support | `#/support` | No |

**Collapsible:** "Collapse/Expand" button at bottom. Active item highlighted.

### 16.4 Page Layout Patterns (DS-01, DS-03)

**List Page Pattern:**
```
[Page Title] [Action Buttons: New/Add/Filter/Export]
[Filter Bar]
[Data Table: sortable, checkbox bulk-select, row overflow menu]
[Pagination]
```

**Master-Detail Pattern (Employees, Loans):**
```
[Left: Entity List with summary cards]  [Right: Selected entity detail]
```

**Tabbed Sub-page Pattern:**
```
[Tab 1] [Tab 2] [Tab 3]
[Content for selected tab]
```
URL reflects tab: `?selectedTab=tabname`

### 16.5 Form Components (DS-02)

| Component | Usage | Key Attributes |
|-----------|-------|----------------|
| Text Input | Names, codes, descriptions | Blue active border; error below field in red |
| Number/Currency | Loan amount, LOP days | ₹ prefix; 2 decimal places; spinner arrows |
| Date Picker | DOB, DOJ, payment dates | dd/MM/yyyy format; calendar popup |
| Dropdown (Simple) | State, Gender, Pay Schedule | Non-searchable |
| Combobox | Employee search, Loan Name | Searchable; real-time filter |
| Toggle Switch | Active/Inactive; statutory enables | Active = colored; Inactive = grey |
| Checkbox | Multi-select options | Right-side label |
| Radio Button | Tax regime, pay frequency | Mutually exclusive group |
| File Upload | Documents, POI | Drag-and-drop + "Choose File"; 50MB max; PDF |
| Text Area | Reason, Banner Message | Resizable; no rich text |

### 16.6 Indian-Specific Format Conventions (DS-02, DS-06)

| Field | Format | Example |
|-------|--------|---------|
| Currency | ₹X,XX,XXX.XX (lakh system) | ₹87,484.00, ₹1,25,000.00 |
| Date | DD/MM/YYYY | 29/05/2026 |
| PAN | AAAAA0000A | ABCDE1234F |
| Aadhaar | XXXX-XXXX-1234 (always masked) | — |
| IFSC | ABCD0123456 | 11 chars: 4 alpha + 0 + 6 |
| EPF Number | AA/AAA/1234567/001 | KA/KAR/1234567/001 |
| ESI Number | NN-NN-NNNNNN-NNN-N | 52-00-123456-000-0001 |
| TAN | AAAA00000A | MUMR12345A |
| Pincode | 6 digits | 560001 |
| Mobile | 10 digits; starts 6/7/8/9 | 9876543210 |
| Financial Year | FY2026-27 | April 2026 – March 2027 |

### 16.7 Color System (DS-06)

**Design system:** Zoho Catalyst.

| Token | Hex (approx) | Usage |
|-------|-------------|-------|
| Primary Blue | #0C6AC9 | Primary buttons, links, active states |
| Zoho Blue | #1565C0 | Top navigation brand color |
| Primary Light | #E8F2FC | Selected row highlights |
| Success Green | #22C55E | PAID badge, Active, Success toast |
| Warning Orange | #F97316 | Pending, Draft, Warning toast |
| Error Red | #EF4444 | Error toast, Rejected, 🔴 flags |
| Info Blue | #3B82F6 | Info toast, informational badges |
| Neutral Grey | #6B7280 | Inactive, Skipped, Closed |
| Page Background | #F8F9FA | Main content area |
| Card Background | #FFFFFF | Cards, tables, panels |
| Sidebar | #1E293B | Left sidebar (dark) |

### 16.8 Status Badge System (DS-06)

| Status | Style | Color |
|--------|-------|-------|
| PAID | Green filled pill | #22C55E |
| ACTIVE | Green filled pill | #22C55E |
| OPEN (Loan) | Blue outline pill | #3B82F6 |
| PENDING | Orange filled pill | #F97316 |
| DRAFT | Grey outline | #9CA3AF |
| SKIPPED | Grey filled | #6B7280 |
| INACTIVE | Grey outline | #9CA3AF |
| REJECTED | Red filled | #EF4444 |
| CLOSED | Grey filled | #6B7280 |

### 16.9 Typography (DS-06)

| Level | Size | Weight | Usage |
|-------|------|--------|-------|
| Page Title | 24px | 600 | Page headings |
| Section Header | 18px | 600 | Card titles, section headers |
| Body | 14px | 400 | Form labels, table content |
| Caption | 12px | 400 | Help text, tooltips |
| Badge | 11–12px | 600 | Status badges |

**Font family:** Lato (primary).

### 16.10 Modal and Drawer Patterns (DS-04)

**Modal Dialog (centered):** Confirmation dialogs, short forms. Backdrop dims. Width 480–640px.

| Modal Type | Usage |
|-----------|-------|
| Confirmation | Delete, Finalize, Cancel — warning icon + consequences |
| Form Modal | Create Loan, Record Repayment, Off-Cycle Pay Run |
| Info/Alert | "Meet the New Reports Centre" welcome dialog |

**Side Drawer (right slide-in):** Entity detail panels. Width 400–500px. Background visible but dimmed. Tabbed content.

| Drawer | Content |
|--------|---------|
| Payslip panel | Days summary, Earnings table, Deductions table, Net Pay, Download |
| Loan detail | Loan fields, Repayment Schedule table |

**All destructive actions require 2-step confirmation** (click → confirmation modal → confirm/cancel).

### 16.11 Notification System (DS-04, DS-05)

**Toast notifications:**

| Type | Color | Auto-dismiss | Duration |
|------|-------|-------------|---------|
| Success | Green (#22C55E) | Yes | 3–4 seconds |
| Error | Red (#EF4444) | No (manual) | Until dismissed |
| Warning | Orange (#F97316) | Yes | 5 seconds |
| Info | Blue (#3B82F6) | Yes | 4 seconds |

**Email notifications (admin → employee):** Payslip ready, IT Declaration reminder, POI submission reminder, Offer Letter.

**Push notifications (employee mobile app):** Salary credited, Reimbursement approved, IT Declaration open, Form 16 available.

### 16.12 Spacing and Sizing (DS-06)

4px base grid:

| Token | Size | Usage |
|-------|------|-------|
| xs | 4px | Icon gaps |
| sm | 8px | Component internal padding |
| md | 16px | Form field spacing |
| lg | 24px | Section spacing |
| xl | 32px | Page section gaps |
| 2xl | 48px | Major section dividers |

**Buttons:** Large (40px), Medium (36px), Small (28px), Icon-only (32×32px).
**Input fields:** Standard (36px), Compact (28px).

---

## Appendix A: Calculation Reference

### A.1 LOP Proration Formula

```
Prorated Component = Monthly Component Amount × (Payable Days / Calendar Days in Month)
```

**Example (May 2026, 2 LOP days, 31 calendar days):**
- Payable Days = 31 − 2 = 29
- Basic: ₹40,000 × 29/31 = ₹37,419 (rounded to ₹37,417 in Zoho)
- HRA: ₹16,000 × 29/31 = ₹14,968 (shown as ₹14,967 in Zoho)
- Fixed Allowance: ₹14,000 (appears NOT prorated — anomaly observed)

### A.2 EPF Calculation

```
EE EPF = 12% × min(PF Wage, ₹15,000)
ER EPS = 8.33% × min(PF Wage, ₹15,000) → capped at ₹1,250
ER EPF = (12% × PF Wage) − ER EPS
EDLI   = 0.5% × min(PF Wage, ₹15,000) → capped at ₹75
```

**PF Wage** = Basic + EPF-flagged components (per component configuration).

**Example (Arjun, Basic ₹40,000 > ₹15,000 ceiling):**
- Computed on ₹15,000
- EE EPF = 12% × ₹15,000 = ₹1,800
- ER EPS = 8.33% × ₹15,000 = ₹1,249.50 → ₹1,250
- ER EPF = ₹1,800 − ₹1,250 = ₹550
- EDLI = 0.5% × ₹15,000 = ₹75

### A.3 ESI Calculation

```
EE ESI = 0.75% × Gross ESI Wage (if gross ≤ ₹21,000/month)
ER ESI = 3.25% × Gross ESI Wage (if gross ≤ ₹21,000/month)
```

**ESI Wage** = Gross salary including all allowances.

### A.4 TDS Computation — New Regime FY2026-27

```
Taxable Income = Annual CTC − Standard Deduction (₹75,000) − NPS 80CCD(2) [employer NPS]
Tax = Apply slab rates (see Section 6.4)
Cess = 4% of Tax
Total Annual Tax = Tax + Cess
Monthly TDS = Total Annual Tax / Remaining Months in FY (adjusted for prior TDS paid)
```

**Section 87A rebate:** If Taxable Income ≤ ₹7,00,000 → Total Tax = ₹0.

**Arjun full calculation:**
```
Annual CTC:           ₹8,40,000
Standard Deduction:  −₹75,000
Net Taxable Income:   ₹7,65,000

Tax on ₹7,65,000:
  0% on ₹0–₹4,00,000     = ₹0
  5% on ₹4,00,001–₹7,65,000 = 5% × ₹3,65,000 = ₹18,250
  Total Tax               = ₹18,250
  87A rebate: Income > ₹7L → No rebate
  4% Cess                 = ₹730
  Total Annual Tax        = ₹18,980
  Monthly TDS             = ₹18,980 / 12 = ₹1,582
```

### A.5 Gratuity Calculation

```
Gratuity = (Monthly Basic / 26) × 15 × Completed Years of Service
```

**Maximum exempt from income tax:** ₹20,00,000.
**Minimum service:** 5 years continuous employment.

**Example (5 years service, Basic ₹40,000):**
```
(₹40,000 / 26) × 15 × 5 = ₹1,538.46 × 15 × 5 = ₹1,15,385
```

### A.6 Loan Perquisite

```
Monthly Perquisite = Outstanding Loan Balance × SBI MCLR / 12
```

**Exempt if:** Medical loan (Rule 18) OR aggregate outstanding < ₹2,00,000.

**Example (Arjun, ₹50,000 balance, SBI MCLR 9.5%):**
```
₹50,000 × 9.5% / 12 = ₹395.83/month
→ But ₹50,000 < ₹2L aggregate limit → Exempt → Perquisite = ₹0
```

### A.7 HRA Exemption Formula (Old Regime — Reference)

```
HRA Exemption = min(
  Actual HRA received,
  50% of Basic (metro cities) or 40% of Basic (non-metro),
  Rent paid − 10% of Basic
)
```

**New Regime:** HRA exemption not available — HRA fully taxable.

### A.8 Kerala PT Computation

**Basis:** Gross monthly salary (not Basic).
**Cycle:** Half-yearly. Deducted September and March.
**For September 2026 expected deductions:**
- Arjun (₹70,000/month gross) → ₹750/half-year
- Priya (₹22,000/month gross) → ₹450/half-year

### A.9 Arrears TDS (Section 192)

```
Revised Annual Income = Original Annual Income + Arrears Amount
Revised Annual Tax = Apply slab rates on revised income
Arrears TDS = Revised Annual Tax − Tax Already Deducted in Prior Months
```

**Section 89(1) relief:** Employee can claim via Form 10E if arrears relate to prior FY.

---

## Appendix B: Cross-Module Data Flow

### B.1 Salary Structure → Pay Run

```
Salary Template (Settings) → Employee Assignment (Employee profile) → Pay Run (auto-populated)
- Component percentages/amounts from template
- Pro-rata flag determines LOP behavior
- EPF/ESI flags determine statutory wage basis
```

### B.2 IT Declaration → Pay Run → Form 24Q

```
Employee Declaration (portal) → POI Approval (Approvals) → TDS Computation (Engine)
→ Pay Run TDS Deduction → TDS Liability (monthly) → Challan (ITNS 281) → Form 24Q
→ TRACES upload → Form 16 (Part A from TRACES, Part B from Zoho)
```

### B.3 EPF → ECR → EPFO

```
Employee EPF (statutory config) → Pay Run EPF computation → ECR Text File (Reports)
→ EPFO Unified Portal upload → PF Challan payment → ECR acknowledgment
```

### B.4 Loans → Pay Run → Perquisite → TDS

```
Loan Creation → Instalment Schedule → Pay Run EMI Deduction
→ Outstanding Balance → Loan Perquisite Report → Taxable Income → TDS computation
```

### B.5 Salary Revision → Arrears → Pay Run

```
Salary Revision (approval) → Backdated Effective Date → Arrears Calculation
→ Arrears Pay Run → Arrears Payslip → TDS on Arrears
```

### B.6 Employee Onboarding Completeness → Pay Run Inclusion

```
Employee Profile completeness (DOB + Father's Name + Address + Bank + Email)
→ Payroll Readiness Gate → Include / Skip in Pay Run
```

### B.7 Reimbursement → Approval → Pay Run

```
Employee Claim (portal) → Approvals > Reimbursements → Approved amount + Payout Month
→ Specific Pay Run inclusion → Payslip earnings line
```

### B.8 Pay Run → Reports → Accounting

```
Pay Run (PAID) → Payroll Journal → GL Entries → Zoho Books (if integrated)
Pay Run (PAID) → Statutory Reports → EPF ECR, ESI challan, PT challan, LWF challan
Pay Run (PAID) → Payslips → Employee Portal (email)
Pay Run (PAID) → Bank Advice → Bank (manual upload or Direct Deposit integration)
```

---

## Appendix C: Gaps and Build Recommendations

### C.1 Critical Compliance Gaps (🔴)

| # | Gap | Source | Recommendation |
|---|-----|--------|---------------|
| C1 | IT Declaration LOCKED — TDS = ₹0 for Arjun (expected ₹1,582/month) | UF-40 | Release IT Declaration; implement auto-lock after configurable date |
| C2 | EPF Number has Karnataka prefix (KA/KAR) but org is in Kerala | UF-08, UF-37 | Validate EPF Number format against work location state code |
| C3 | TAN jurisdiction (MUMR = Mumbai) mismatches Kerala location | UF-95 | Warning when TAN jurisdiction does not match org state |
| C4 | PT Number blank — PT challan generation blocked | UF-39, UF-71 | Mandatory PT Number field with format validation |
| C5 | "Tax Deductor not found" — Deductor Name not configured | UF-74, UF-95 | Block Form 16 issuance until Deductor Name is selected |
| C6 | AO Code blank — Form 24Q may generate with incomplete data | UF-95 | Warning before Form 24Q generation if AO Code missing |
| C7 | Prior Payroll not enabled — mid-year joiners' TDS may be understated | UF-84, UF-87 | Prompt admin to configure Prior Payroll for mid-year joiners |
| C8 | "Allow employees to switch tax regimes" = ON — old regime logic exposed | UF-85 | For V1 new-regime-only build: disable regime switching |
| C9 | IT Declaration URL contains `?tax_regime=with_exemptions` — implies old regime default | UF-25, UF-27 | Verify and fix: new regime org should not default to old regime view |

### C.2 Data Quality Gaps

| # | Gap | Source |
|---|-----|--------|
| D1 | Priya's PAN = "-" but she is included in pay runs | UF-24 |
| D2 | Employee ID is mutable post-creation (data integrity risk) | UF-23 |
| D3 | No approval workflow for bank detail changes | UF-24 |
| D4 | No effective date for statutory toggle changes (EPF/ESI/PT/LWF per employee) | UF-16 |
| D5 | No audit trail visible for statutory toggle changes | UF-16 |
| D6 | Onboarding checklist marks Step 7 complete on page visit, not actual config | UF-84, UF-87 |
| D7 | SBI MCLR rate for perquisite computation — no configuration UI found | UF-66, UF-80 |
| D8 | Fixed Allowance not prorated on LOP (₹13,100 in May despite 2 LOP days) | UF-45 |

### C.3 Feature Gaps (Missing Functionality)

| # | Gap | Source |
|---|-----|--------|
| F1 | No per-employee Documents tab — Documents is org-global only | UF-17 |
| F2 | No guard preventing deletion of in-use salary components | UF-07 |
| F3 | No "Mark as Inactive" suggestion when deleting components | UF-07 |
| F4 | No force-include override for skipped employees | UF-41, UF-49 |
| F5 | No "complete onboarding" link from pay run skipped employee row | UF-41 |
| F6 | No per-employee LWF opt-out | UF-11 |
| F7 | No instalment schedule preview before saving loan | UF-63 |
| F8 | UI says "IT Rules, 2026" for perquisite exemption — likely should be "1962" | UF-63 |
| F9 | No Reason field when creating Off-Cycle Pay Run | UF-52 |
| F10 | No approval/audit trail for salary revision when multi-level not configured | UF-62 |
| F11 | Revision run behavior (replace vs supplement payslip) not confirmed | UF-57 |
| F12 | Post-reversal state (Draft vs Approved) not confirmed | UF-58 |
| F13 | LWF Registration Number field not visible in settings | UF-72 |
| F14 | FBP entirely inactive (all reimbursement components = ₹0 inactive) | UF-87, UF-88 |
| F15 | Approval history — no dedicated view in Approvals module | UF-62 |
| F16 | "Hold Salary (Non Taxable)" semantics unclear | UF-86 |
| F17 | No NPS employer contribution component visible | UF-86 |
| F18 | Employee portal web URL not confirmed | UF-85 |
| F19 | Employee invite flow not tested | UF-85 |
| F20 | Transport Allowance hardcoded at ₹1,600 (legacy pre-2018 amount) | UF-86 |

### C.4 Test Coverage Gaps (Flows Not Executed)

| # | Flow | Source |
|---|------|--------|
| T1 | FnF pay run — no exited employees in demo | UF-34, UF-81 |
| T2 | Gratuity auto-calculation — admin enters manually | UF-35 |
| T3 | Arrears pay run — revision still pending approval | UF-54 |
| T4 | Revision pay run — no errors in May run | UF-57 |
| T5 | Delete Recorded Payment — not clicked (risk) | UF-58 |
| T6 | POI approval — no POI submissions | UF-31, UF-90 |
| T7 | Form 24Q generation — TAN configured but Deductor missing | UF-73 |
| T8 | Form 16 — blocked by missing Deductor Name | UF-74, UF-75, UF-76 |
| T9 | Employee portal login — requires employee credentials | UF-85 |
| T10 | Reimbursement claim — all components inactive | UF-88 |
| T11 | Loan EMI in pay run — first EMI July 2026 (audit session May) | UF-64 |
| T12 | Bank Advice download format — button not clicked | UF-51 |
| T13 | Challan recording — no TDS liability to pay | UF-89 |
| T14 | VPF deduction in pay run — no employees with VPF | UF-87 |
| T15 | Activity Logs report — not individually opened | UF-83 |

### C.5 Build Recommendations for Custom System

Based on this audit of Zoho Payroll, the following recommendations apply to building the custom Indian Payroll SaaS:

1. **Payroll readiness gate as composite check:** Implement the same multi-field gate (DOB + Father's Name + Address + Bank + Email + Salary Structure). Expose a "completion percentage" indicator per employee.

2. **Fixed Allowance as residual absorber:** Implement Fixed Allowance as `CTC − Sum(all other components)`. Make it immutable and auto-computed. Never allow manual entry.

3. **EPF "if PF wage < 15k" conditional flag:** Implement component-level EPF inclusion with the ceiling-aware conditional logic.

4. **IT Declaration lifecycle as explicit state machine:** Draft → Released → Submitted → POI Window Open → Approved → Locked. Each state transition requires explicit admin action. No implicit transitions.

5. **Kerala PT half-yearly:** Implement PT with state-specific frequency (monthly vs half-yearly). Kerala: deduct only in September and March. Compute on gross salary against half-yearly slabs.

6. **TDS spreading method for arrears and bonus:** When adding arrears/bonus income, spread revised tax across remaining months. Do not flat-rate the bonus month.

7. **Statutory number validation with state-matching:** EPF Number state prefix should match work location state. TAN jurisdiction should be flagged if it does not match org registered state.

8. **Bank detail change approval workflow:** Implement mandatory approval for bank account changes. Risk: salary misdirection without approval gate.

9. **Hard-delete guard for salary components:** Check association before delete. If component is in use, block delete and offer "Mark as Inactive" instead.

10. **Loan perquisite with configurable SBI MCLR:** Provide admin-configurable MCLR rate (update quarterly). Auto-compute perquisite monthly and add to taxable income.

11. **Proration of Fixed Allowance on LOP:** Decide policy (Zoho does not prorate Fixed Allowance — anomaly). Recommend prorating all components consistently.

12. **Prior employer YTD import:** Enable before first pay run. Collect: prior employer name, TAN, salary paid, TDS deducted, PF contributions. Feed into TDS computation for remaining FY months.

---

## Appendix D: Key Entities and Fields

### D.1 Organisation Entity

| Field | Type | Notes |
|-------|------|-------|
| Name | Text | "lerno" in demo |
| PAN | Text | Format: AAAAA0000A |
| TAN | Text | Format: AAAA00000A |
| AO Code | Composite | 4 parts |
| State | Dropdown | Determines PT, LWF defaults |
| Fiscal Year | Auto | April–March |
| Pay Schedule | FK | Monthly, weekly, etc. |
| Tax Regime | Radio | New/Old (V1: New only) |

### D.2 Employee Entity

| Field | Type | Required for Payroll |
|-------|------|---------------------|
| Employee ID | Text (auto) | Yes |
| First Name | Text | Yes |
| Last Name | Text | No |
| Date of Joining | Date | Yes |
| Work Email | Email | Yes |
| Gender | Enum | No |
| Work Location | FK | Yes (PT slab) |
| Designation | Text | No |
| Department | Text | No |
| Date of Birth | Date | Yes (payroll gate) |
| Father's Name | Text | Yes (payroll gate) |
| PAN | Text | Recommended; "-" allowed |
| Personal Email | Email | Yes (payroll gate) |
| Mobile | Phone | No |
| Permanent Address | Text | Yes (payroll gate) |
| Bank Account Number | Text (masked) | Yes (payroll gate) |
| Bank IFSC | Text | Yes |
| Payment Mode | Enum | Yes |
| Salary Structure | FK | Yes |
| EPF Enabled | Boolean | Per-employee |
| ESI Enabled | Boolean | Per-employee |
| PT Enabled | Boolean | Per-employee |
| LWF Enabled | Boolean | Per-employee |
| Tax Regime | Enum | New/Old per employee |

### D.3 Salary Component Entity

| Field | Type | Notes |
|-------|------|-------|
| Name | Text | Display name |
| Name in Payslip | Text | Optional override |
| Earning Type | Enum | Fixed 33 types; immutable post-creation |
| Pay Type | Enum | Fixed/Variable |
| Calculation Type | Enum | % of CTC, % of Basic, Flat Amount |
| Consider for EPF | Boolean | + conditional "if PF wage < 15k" variant |
| Consider for ESI | Boolean | |
| Pro-rata | Boolean | LOP deduction |
| Status | Enum | Active/Inactive |

### D.4 Pay Run Entity

| Field | Type | Notes |
|-------|------|-------|
| ID | Text | 3848927000000034159 format (Zoho internal) |
| Type | Enum | Regular, Off-Cycle, Bonus, Arrears, FnF, Revision |
| Period Start | Date | First day of pay period |
| Period End | Date | Last day of pay period |
| Pay Date | Date | When payment is made |
| Status | Enum | Draft/Under Review/Approved/Paid |
| Total Net Pay | Decimal | Sum of all employee net pays |
| Employee Count | Int | Included employees |
| Skipped Count | Int | Skipped employees |

### D.5 Loan Entity

| Field | Type | Notes |
|-------|------|-------|
| Loan Number | Text (auto) | LOAN-00001 format |
| Loan Name/Type | FK | Personal, Emergency, etc. |
| Employee | FK | |
| Loan Amount | Decimal | |
| Disbursement Date | Date | |
| Number of Instalments | Int | |
| Instalment Amount | Decimal | Auto-computed |
| First Instalment Date | Date | |
| Reason | Text | |
| Perquisite Rate | Decimal | % interest employer charges |
| Exempt from Perquisite | Boolean | Rule 15(5) |
| Status | Enum | Open/Paused/Closed |
| Amount Repaid | Decimal | Running total |
| Remaining Amount | Decimal | Auto-computed |

### D.6 TDS Liability Entity

| Field | Type | Notes |
|-------|------|-------|
| Period | Month | e.g., May 2026 |
| Quarter | Enum | Q1/Q2/Q3/Q4 |
| TDS Amount | Decimal | Total deducted in period |
| Status | Enum | Unpaid/Paid |
| Challan ID | FK | When paid via challan |
| Due Date | Date | 7th of following month (Mar → 30 Apr) |

### D.7 Statutory Configuration Entities

| Entity | Key Fields |
|--------|-----------|
| EPF Config | EPF Number, EE rate, ER rate, EDLI rate, wage ceiling |
| ESI Config | ESI Number, EE rate (0.75%), ER rate (3.25%), wage ceiling |
| PT Config | PT Number, state, frequency, slabs |
| LWF Config | Registration Number, state, EE amount, ER amount, frequency |
| Tax Deductor | TAN, PAN, AO Code, Deductor Name, Deductor Type |

---

*Compiled from 102 audit files (UF-01 through UF-96, DS-01 through DS-06). Audit date: 2026-05-16.*
*All monetary values in Indian Rupees (₹). Calculations use statutory provisions for FY2026-27.*
