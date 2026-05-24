# Indian Payroll SaaS — System Overview

> Simple language. No jargon assumed. Written for anyone trying to understand what this system does and how its pieces connect.

---

## What This System Is

A software product that companies use to pay their employees. The company signs up, enters their details, and every month the system calculates how much each employee gets paid, deducts the right taxes and contributions, and produces payslips and filings.

Multiple companies (called **tenants**) use the same software, but each company's data is completely separate from every other company's data — they can never see or affect each other.

---

## The Big Picture: How Setup Flows Into Payroll

```
Company signs up
       ↓
Company Profile (legal name, PAN, TAN)
       ↓
Work Locations → Org Structure (Departments, Designations, etc.)
       ↓
Pay Schedule (when to pay, how to calculate days)
       ↓
Salary Components (what pay elements exist)
       ↓
Salary Structures (which components apply to which group)
       ↓
Statutory Settings (EPF, ESI, PT, LWF, Bonus — what govt deductions to apply)
       ↓
Employees (assigned a salary structure + work location)
       ↓
Monthly Payroll Run → Payslips + Tax filings
```

Every layer feeds the next. Missing or wrong config at any layer produces wrong pay.

---

## Tenants (Companies)

Each company that uses the system is a tenant. When a company is created:
- A separate data space (database schema) is created just for them
- A default admin user is created and sent a setup email
- Default config rows are pre-created (pay schedule, statutory config) so the company can start immediately

**Conditions:**
- Tenants can be Active, Suspended, or Inactive
- A suspended tenant's users cannot log in
- Tenant data is always isolated — no query can cross into another tenant's data

---

## Company Profile (OrgProfile)

What it stores: company name, legal name, PAN, GSTIN, address, logo, website, incorporation date, and TDS filing details.

**TDS filing details** (in the Tax Details settings page) are needed for government tax filings:
- **TAN** — Tax Deduction Account Number. Every company that deducts salary tax must have this.
- **AO Code** — A 4-part code (Area Code / AO Type / Range Code / AO Number) that identifies which Income Tax office handles this company's filings.
- **Deductor info** — Name, type (Company/Individual/etc.), and for individuals: father's name and designation.

These fields appear on Form 24Q (quarterly TDS return) and Form 16 (annual tax certificate given to employees). They don't affect salary calculations — they only appear on official filings.

---

## Work Locations

Physical offices and branches where employees work.

Each work location stores:
- Name, address, city, state, pincode
- **PT Registration Number** — each state has a Professional Tax authority. Companies that deduct PT must register with each state and store that registration number per office.
- Whether it's active or not
- How many employees are assigned to it

**Why it matters for payroll:**
- Professional Tax (PT) is determined by which state the employee's work location is in. Same employee, different office state = different PT deduction.
- LWF (Labour Welfare Fund) is also state-specific and uses the work location's state.

**Rules:**
- State is fixed after creation (cannot change — changing it would affect historical pay records).
- Cannot delete a location that has employees assigned.

---

## Org Structure

Five independent lists that describe how a company is organised. Each is a flat list (no hierarchy):

| Setting | What It Is |
|---|---|
| **Departments** | Functional groups (Engineering, Sales, HR, etc.) |
| **Designations** | Job titles (Manager, Engineer, Analyst, etc.) |
| **Cost Centres** | Budget buckets (used for accounting — which budget pays which salary) |
| **Business Units** | Divisions within a company (India Ops, US Division, etc.) |
| **Work Locations** | Physical offices (described above) |

Each employee is tagged with one of each. These tags don't directly change salary calculations in v1, but they're used for filtering payroll reports and are required for some government filings.

---

## Pay Schedule

Org-level settings that define *how* and *when* salaries are calculated. One pay schedule per company.

**Work Week:**
Which days are working days (e.g. Mon–Fri). Used when an employee joins or leaves mid-month — the system counts only working days to calculate partial-month pay.

**Salary Calculation Method:**
- *Actual days in a month* — divide monthly salary by 28/29/30/31 (whatever that month has). Most common.
- *Fixed working days per month* — divide by a fixed number (e.g. 26) every month regardless of how many days the month actually has.

This choice is **locked** after the first payroll run is processed and cannot be changed. Changing it would make old and new payslips inconsistent.

**Pay Date:**
- Last day of every month, OR
- A specific day (e.g. always the 28th)

**First Pay Period:**
The month and year of the company's first payroll run. The UI shows a preview of upcoming pay dates (next 3 months) once this is set, so the admin can verify the dates look right before running payroll.

---

## Salary Components

The building blocks of an employee's salary. Every pay element — from Basic Salary to Mobile Allowance to a loan deduction — is a salary component.

### Categories

| Category | What It Is | Example |
|---|---|---|
| **Earning** | Money paid to the employee | Basic, HRA, Bonus |
| **Deduction** | Money taken from the employee | Loan repayment, Advance recovery |
| **Reimbursement** | Expense refund (tax-free up to a limit) | Medical, Internet, Food |
| **Benefit** | Employer contribution on behalf of employee | Employer PF contribution, NPS |
| **Correction** | Adjustment to fix a previous earning | Arrears, corrections |

### Earning Types

Each earning component is one of many predefined types (Basic, HRA, LTA, Conveyance, etc.) or "Other/NotInList" for custom ones.

**Special system component:** *Fixed Allowance* — this is the leftover absorber. If a company defines a salary structure where CTC = ₹10L/year and all named components (Basic, HRA, etc.) only add up to ₹8L, Fixed Allowance automatically gets the remaining ₹2L. Tenants cannot create this manually — it's system-managed.

### Formulas

Each earning in a salary structure can be calculated as:
- **Fixed Amount** — a set rupee value (e.g. ₹2,000/month)
- **% of Basic** — a percentage of the Basic salary component
- **% of Gross** — a percentage of total gross salary
- **% of CTC** — a percentage of the annual Cost to Company
- **Residual CTC** — only for Fixed Allowance; automatically fills the gap

### Rules That Lock After Use

Some fields on a component cannot be changed once employees are using it:
- **EarningType** — locked after creation (changing it would misclassify historical pay)
- **Formula type, EPF/ESI flags, pro-rata flag** — locked once any employee's salary structure uses this component

This is to prevent retroactive changes to how old payslips were computed.

### EPF and ESI Flags

Each earning component has two flags:
- **Consider for EPF** — should this earning be included when calculating PF contribution?
- **Consider for ESI** — should this earning be included when calculating ESI contribution?

These directly affect how much PF and ESI gets deducted. Getting these wrong means wrong deductions for all employees using that component.

---

## Salary Structures (Templates)

A salary structure is a named bundle of salary components with their formulas and amounts. Think of it as a pay package template.

Example:
- **Structure: "Engineer L3"**
  - Basic: 40% of CTC
  - HRA: 50% of Basic
  - Special Allowance: Residual CTC
  - Mobile Allowance: Fixed ₹1,000
  - Employee PF: 12% of PF Wage (deduction)

Multiple structures can exist (one for each grade/level/region). Each employee is assigned one structure. When payroll runs, the system uses that employee's structure to compute their earnings.

**One component, many structures:** The same "Basic Salary" component can appear in multiple structures with different formulas. The formula is stored per-structure-slot, not on the component itself.

**Impact on payroll:** When an employee's structure is changed (e.g. promotion), the new structure applies from the effective date. Old payslips remain unchanged — they were computed with the old structure.

---

## Statutory Components

Indian law requires employers to make several contributions and deductions. These are configured per company in the Statutory Components settings.

### EPF — Employees' Provident Fund

A retirement savings scheme. Both the employee and employer contribute each month.

**Who must enroll:** Companies with 20+ employees. Employees earning ≤ ₹15,000/month PF wage must be enrolled. Above that, enrollment is optional.

**How it works:**
- Employee contributes 12% of their PF wage every month (deducted from salary)
- Employer contributes ~12% (split: 3.67% to EPF, 8.33% to EPS pension fund)
- Employer also pays small admin charges (EDLI insurance, admin fee)

**Settings the company configures:**
- EPF Establishment Code (their government registration number)
- Contribution rate options (fixed 12% of ₹15,000 cap, or actual PF wage up to no cap)
- Whether employer contributions are included in CTC or on top of it
- Whether employees can override their own PF amount

**Impact on payroll:** If EPF is enabled, every eligible employee gets a PF deduction. The employer's share also appears in cost calculations. Wrong rate settings = wrong deductions for all employees.

### ESI — Employees' State Insurance

Health insurance for lower-salaried employees, run by the government.

**Who must enroll:** Companies with 10+ employees. Employees earning ≤ ₹21,000/month gross wage are covered.

**How it works:**
- Employee contributes 0.75% of gross wage
- Employer contributes 3.25% of gross wage

**Settings the company configures:**
- ESI Establishment Code
- Whether the company is in an ESI notified area (some locations are excluded)

**Impact on payroll:** Employees above ₹21,000 gross are automatically excluded each month. If someone's salary changes and crosses the threshold, the system handles the transition.

### Professional Tax (PT)

A state-level tax on employment. Not all states have it — only: Maharashtra, Karnataka, West Bengal, Tamil Nadu, Telangana, Andhra Pradesh, Kerala, Gujarat, Madhya Pradesh (and a few others).

**How it works:**
- The employee pays PT (employer deducts it)
- Amount depends on the employee's gross salary and which state their work location is in
- Rates are in slabs (e.g. earning ₹10,001–₹15,000 → ₹150/month; above ₹15,000 → ₹200/month)
- Maharashtra has a February surcharge (extra amount in February)
- Some states (Maharashtra) have gender-based slabs

**How slabs are managed:**
- Slabs are stored with an effective date
- When the government revises PT rates (which happens occasionally), new slabs are added with a future effective date
- Old slabs are never deleted — they're used to reproduce old payslips accurately
- The system picks the slab set whose effective date is the most recent date on or before the pay period

**Settings the company configures:**
- PT Registration Number per work location (each office in a PT state needs its own registration)
- PT slabs per state (via Revise Slabs — sets effective date + new slab table)

**Impact on payroll:** PT is deducted from employees based on their work location state. Same salary, different state = different PT.

### LWF — Labour Welfare Fund

A small contribution to a state welfare fund for workers. Only a few states have it.

**How it works:**
- Fixed amounts per employee and employer (e.g. Karnataka: employee ₹20, employer ₹40 per year)
- Some states use percentage-based rates (Haryana)
- Deducted at specific intervals: monthly, half-yearly (June + December), or annually (usually December)
- Some states only apply it to employees below a wage threshold

**Impact on payroll:** Deducted only in the applicable months. If June is the deduction month, employees see an LWF deduction in June's payslip only.

### Statutory Bonus

Under the Payment of Bonus Act 1965, companies with 20+ employees must pay an annual bonus to employees earning ≤ ₹21,000/month.

**How it works:**
- Minimum bonus: 8.33% of annual wages
- Maximum bonus: 20% of annual wages
- The base for calculation: `max(₹7,000, state minimum wage) × 12`

**Settings the company configures:**
- Bonus Rate (8.33% to 20%)
- Payout Mode: Monthly (spread across 12 months) or Yearly (paid in a specific month)
- Payout Month (if Yearly)

**Impact on payroll:** If monthly mode, a bonus amount is added to each payslip. If yearly, it appears only in the chosen payout month.

---

## Income Tax (TDS)

India uses a Pay-As-You-Earn system. Employers must estimate each employee's annual tax liability at the start of the year and deduct it proportionally each month (Tax Deducted at Source = TDS).

**How the system handles it:**
- Tax slabs are pre-loaded in the database (not hardcoded in the calculation engine)
- V1 supports only the New Tax Regime (simplified slabs, fewer exemptions)
- Standard deduction (₹75,000 for FY2025-26) is automatically applied
- Rebate u/s 87A: employees with taxable income ≤ ₹12L pay zero tax (rebate cancels it)
- Employer contributions to EPF + NPS combined are taxable if they exceed ₹7.5L/year

**Impact on payroll:** TDS is deducted every month. In the last 3 months of the financial year, the system recalculates the full-year picture and adjusts each month's deduction so the total hits exactly the right annual number.

---

## Payroll Run

The monthly process that produces payslips.

### Lifecycle

```
Pending → Processing → Draft → Finalised
                  ↘ Failed
```

- **Pending** — created, not yet started
- **Processing** — calculation running in background
- **Draft** — calculated, admin can review before approving
- **Finalised** — approved, payslips locked, cannot change
- **Failed** — calculation error (logged with reason)

A finalised payroll run can be unlocked (with a reason), which moves it back to Draft for correction. This is an audit trail — the unlock reason is stored.

### What Gets Calculated

For each employee in a pay period:

1. **Gross earnings** — each component formula evaluated (fixed, % of basic, % of CTC, etc.)
2. **Pro-rata** — if employee joined or left mid-month, earnings are reduced proportionally based on working days
3. **EPF deduction** — if employee is eligible and EPF is enabled
4. **ESI deduction** — if employee earns ≤ ₹21,000 gross and ESI is enabled
5. **PT deduction** — based on work location state and current PT slabs
6. **LWF deduction** — only in deduction months, if state has LWF
7. **TDS** — monthly income tax installment
8. **Net pay** — Gross − all deductions

### What Settings Feed In

| Setting | What It Drives |
|---|---|
| Pay Schedule → Work Week | Pro-rata days for mid-month joiners/leavers |
| Pay Schedule → Calculation Method | How daily rate is computed |
| Salary Structure assigned to employee | Which components, which formulas, which amounts |
| EPF config | PF deduction + employer cost |
| ESI config | ESI deduction + employer cost |
| PT slabs for employee's state | PT deduction amount |
| LWF config for employee's state | LWF deduction (in applicable months) |
| Income Tax config + slabs | Monthly TDS amount |
| Statutory Bonus config | Whether bonus appears in payslip |

### Immutability Rule

**Once a payroll run is finalised, it is permanently locked.** The numbers in it never change. If something was wrong, you unlock it → fix the source (salary structure, config, etc.) → re-run. This preserves audit integrity — the payslip an employee received matches what the system recorded.

If configuration changes (e.g. EPF rate changed, salary structure updated) after a run is finalised, those changes only affect *future* runs. Past runs use the config that was in effect when they were processed.

---

## Multi-Tenancy: How Data Is Isolated

Each company's data lives in its own PostgreSQL schema (like a private filing cabinet). Every database query is automatically scoped to the current tenant's schema. There is no shared table that mixes data from different companies.

This means:
- Company A cannot see Company B's employees, payslips, or settings
- A bug in Company A's payroll run cannot affect Company B's data
- Each company gets their own migration history (schema changes are applied per-tenant)

---

## Authentication and Access

- Users log in with email + password
- A JWT token is issued containing the user's identity and their company (tenant)
- Every API request checks: does this token's tenant match the subdomain in the URL? If not, access is denied.
- Passwords are never stored — only a cryptographic hash
- Sensitive fields (employee PAN, Aadhaar, bank account) are encrypted before storage and never appear in logs

---

## What Is Not Yet Built (Deferred)

| Area | Status |
|---|---|
| Employee management (hire, manage, terminate) | Entity exists; full UI + API not built |
| Payroll run execution engine | PayrollRun entity exists; calculation engine not wired |
| Payslip generation | Not built |
| Form 16 / Form 24Q generation | Not built |
| Employee self-service portal | Not built |
| Old tax regime (pre-2020 rules) | Explicitly deferred; will not be in v1 |
| Leave management | Not built |
| Expense management | Not built |

---

## Key Constraints That Cannot Be Changed After Use

These rules exist because changing them would make historical records inconsistent:

| What | When It Locks | Why |
|---|---|---|
| Pay Schedule — Work Week | After first payroll run | Old pro-rata calculations would be wrong |
| Pay Schedule — Calculation Method | After first payroll run | Daily rate formula would change |
| Salary Component — Earning Type | After creation | Changing type reclassifies all past pay |
| Salary Component — Formula/EPF/ESI flags | After any employee uses it | Would retroactively change deductions |
| Work Location — State | After creation | PT/LWF history is state-specific |
| Payroll Run | After finalisation | Payslips are legal documents |
