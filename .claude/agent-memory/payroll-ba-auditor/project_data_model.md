---
name: project-data-model
description: All domain entities, DB tables, value objects, enums, and relationships discovered in the codebase
metadata:
  type: project
---

## Platform DB Schema (public schema, ASP.NET Identity + OpenIddict)

### AspNetUsers (ApplicationUser)
- id: uuid PK
- tenant_id: uuid nullable (null for SuperAdmin)
- employee_id: uuid nullable (links to tenant schema employee)
- is_super_admin: bool default false
- Standard Identity fields: user_name, email, password_hash, email_confirmed, lockout_end, lockout_enabled, access_failed_count, two_factor_enabled

### Tenants (stored in public schema)
- id: uuid PK
- display_name: varchar(100)
- slug: varchar (unique, lowercase-alphanumeric-hyphens, 3-63 chars)
- schema: varchar (derived: tenant_{slug_underscored})
- is_active: bool
- created_at: timestamptz

### OpenIddict tables: OpenIddictApplications, OpenIddictAuthorizations, OpenIddictScopes, OpenIddictTokens
### data_protection_keys table

## Tenant DB Schema (per-tenant schema, e.g. tenant_acme_corp)

### employees
- id: uuid PK
- first_name: varchar(100) NOT NULL
- last_name: varchar(100) NOT NULL
- employee_code: varchar(50) NOT NULL, UNIQUE per tenant
- encrypted_pan: text NOT NULL (AES-256 ciphertext)
- encrypted_aadhaar: text nullable (AES-256 ciphertext)
- encrypted_bank_account: text nullable (AES-256 ciphertext)
- encrypted_ifsc: text nullable (AES-256 ciphertext)
- uan: varchar(12) nullable (Universal Account Number for PF)
- esicip_number: varchar(17) nullable (ESI IP number)
- date_of_birth: date NOT NULL
- gender: text (enum: Male, Female, Other)
- date_of_joining: date NOT NULL
- date_of_leaving: date nullable
- employment_type: text (enum: FullTime, PartTime, Contract, Intern)
- status: text (enum: Active — default on create; other states TBD)
- work_state: text (ISO 3166-2:IN two-letter code)
- pf_opt_out: bool default false
- is_pwd: bool default false (Person with Disability — PIT Act exemption)
- tenant_id, department_id (FK), designation_id (FK), branch_id (FK nullable), cost_centre_id (FK nullable)
- Soft delete + full audit trail (created_by, updated_by, deleted_by, timestamps)
- Indexes: ix_employees_tenant_id_employee_code (UNIQUE), ix_employees_tenant_id

### branches
- id, name: varchar(200), state: text (IndianState enum), tenant_id
- Soft delete + audit trail
- Index: ix_branches_tenant_id

### departments
- id, name: varchar(200), code: varchar(50) nullable, tenant_id
- parent_department_id: uuid nullable (self-referential — hierarchy supported in DB but not yet in UI)
- Soft delete + audit trail
- Index: ix_departments_tenant_id

### designations
- id, name: varchar(200), tenant_id
- Soft delete + audit trail

### cost_centres
- id, name: varchar(200), code: varchar(50) nullable, tenant_id
- Soft delete + audit trail

### salary_components
- id, name: varchar(200), code: varchar(50) UNIQUE per tenant
- formula_type: text (enum: Fixed, PercentOfBasic, PercentOfGross, PercentOfCTC)
- fixed_amount: numeric(18,4) nullable
- percentage: numeric(7,4) nullable
- is_taxable: bool
- is_system_component: bool
- Soft delete + audit trail

### salary_structures (EmployeeSalaryStructure)
- id, employee_id (FK), tenant_id
- annual_ctc: numeric(18,4) NOT NULL
- effective_from: date NOT NULL
- effective_to: date nullable
- Soft delete + audit trail
- Index: ix_salary_structures_employee_id_effective_from

### payroll_runs
- id, tenant_id
- pay_period_year: int, pay_period_month: int (composite PayPeriod value object)
- status: text (enum: Pending, Processing, Draft, Finalised, Failed)
- variable_inputs_file_key: text nullable (MinIO S3 key)
- started_at, completed_at: timestamptz nullable
- failure_reason: varchar(2000) nullable
- unlock_reason: varchar(2000) nullable
- employee_count: int
- Soft delete + audit trail
- Index: ix_payroll_runs_tenant_id_status (NOT unique — allows multiple runs per period if one fails)

### statutory_toggles
- id, tenant_id
- module: text (enum: PF, ESI, PT, LWF, TDS)
- is_enabled: bool
- UNIQUE: ix_statutory_toggles_tenant_id_module (one toggle per module per tenant)
- Soft delete + audit trail

### audit_logs
- id, tenant_id, action: varchar(200), entity_type: varchar(200), entity_id: uuid
- old_value: text nullable, new_value: text nullable
- performed_by: uuid, occurred_at: timestamptz, ip_address: varchar(45)
- Indexes: by entity_type+entity_id, by tenant_id+occurred_at

## Value Objects (Domain Layer)
- PAN: regex ^[A-Z]{5}[0-9]{4}[A-Z]{1}$ — validated at domain construction
- Aadhaar: exactly 12 digits; always masked in output (XXXX-XXXX-NNNN)
- Money: decimal precision (TBD — likely wraps decimal)
- EmployeeCode: (TBD)
- PayPeriod(Year, Month): Indian FY April-March. FY2026 = Apr2025-Mar2026. MonthsRemaining computed.

## Enums
- Gender: Male, Female, Other
- EmploymentType: FullTime, PartTime, Contract, Intern (in our code; Zoho has none)
- EmployeeStatus: Active, OnNoticePeriod, Exited, Inactive
- IndianState: 37 ISO 3166-2:IN two-letter codes (AN to WB)
- PayFrequency: (TBD — entity exists in domain)
- ComponentFormulaType: Fixed, PercentOfBasic, PercentOfGross, PercentOfCTC
- PayrollRunStatus: Pending, Processing, Draft, Finalised, Failed
- PayrollRunType: Regular, FullAndFinal (add to distinguish F&F pay runs)
- StatutoryModule: PF, ESI, PT, LWF, TDS
- ExitReason: TerminatedByEmployer, TerminationByDeath, TerminationByDisability, ResignedByEmployee
- FinalPayMode: RegularSchedule, CustomDate

## New Entities Required (from Zoho reference audit — Employees Module)

### ScheduledEarning (org-level master)
- id, tenant_id, name, amount: decimal, earning_type, is_taxable: bool
- effective_from, effective_to dates (optional)
- Soft delete + audit trail

### EmployeeScheduledEarning (junction)
- id, employee_id, scheduled_earning_id, start_date, end_date (nullable)
- Payroll engine includes this in gross for applicable pay periods

### Deduction (org-level master)
- id, tenant_id, name, amount: decimal, is_pre_tax: bool
- Soft delete + audit trail
- Note: Zoho models Benefits as pre-tax deductions (is_pre_tax=true)

### EmployeeDeduction (junction)
- id, employee_id, deduction_id, amount: decimal, is_pre_tax: bool, start_date, end_date

### PriorEmployerYtd
- id, employee_id, financial_year (e.g. "2025-26")
- employer_name, period_from, period_to
- gross_salary, standard_deduction_claimed (0 or 75000), professional_tax_paid, tds_deducted, other_income
- entered_by, entered_at (audit)
- UNIQUE per (employee_id, employer_name, period_from) — allow multiple prior employers per FY

### EmployeeExit
- id, employee_id, last_working_day, reason (ExitReason enum)
- final_pay_mode (FinalPayMode enum), final_pay_date (nullable — for CustomDate mode)
- personal_email (nullable), notes (nullable)
- initiated_by, initiated_at (audit)

### FullAndFinalSettlement
- id, employee_exit_id, payroll_run_id
- prorated_salary, earned_leave_days, leave_encashment_amount, gratuity_amount
- notice_pay_paid, notice_pay_recovered, loan_deduction, tds_final, net_payable
- status (Computed, Approved, Disbursed)

### SalaryRevision (new — for dated hike tracking)
- id, employee_id, previous_annual_ctc, new_annual_ctc, effective_from
- reason (text), initiated_by, initiated_at
- Creates a new SalaryStructure record effective from the revision date
- Previous SalaryStructure gets effective_to = revision effective_from - 1 day

## Pay Run Data Model Gaps (from Zoho reference audit — Session 7)

### PayrollRunStatus — Missing States
Current enum: `Pending, Processing, Draft, Finalised, Failed`
Zoho-observed states require adding:
- `PaymentDue` — approved but payment not yet recorded (between Draft and Finalised)
- `Paid` — payment recorded (maps to our "Finalised" conceptually — but Zoho keeps them distinct)
- Recommendation: rename `Finalised` → `Paid` and add `PaymentDue` between `Draft` and `Paid`

### PayrollRunType — Missing Types
Current enum: `Regular, FullAndFinal`
Zoho has 7+ distinct run types:
- `Regular` — standard monthly
- `Past` — back-dated run
- `FinalSettlement` — single F&F
- `OneTimePayout` — ad-hoc payment
- `OffCycle` — supplemental run outside schedule
- `BulkFinalSettlement` — multiple F&F in one run
- `Resettlement` — correction/revision to paid run

### PayRunEmployee (Junction Entity — MISSING in our build)
Required entity linking payroll_runs ↔ employees, per-run per-employee data:

```
payrun_employees
- id: uuid PK
- payroll_run_id: uuid FK → payroll_runs
- employee_id: uuid FK → employees
- status: text enum (Included, Skipped, Missing)
- lop_days: numeric(5,2) nullable (Loss of Pay days for this run)
- tds_override_amount: numeric(18,4) nullable
- tds_override_reason: text nullable (mandatory when override set)
- gross_pay: numeric(18,4) nullable (computed)
- net_pay: numeric(18,4) nullable (computed)
- skip_reason: text nullable (mandatory when status=Skipped)
- pay_arrear_later: bool default false (for skipped new joiners)
- Soft delete + audit trail
- UNIQUE (payroll_run_id, employee_id)
```

### PayRunComponentLine (Line Items — MISSING in our build)
Per-employee per-run per-component breakdown:

```
payrun_component_lines
- id: uuid PK
- payrun_employee_id: uuid FK → payrun_employees
- salary_component_id: uuid FK → salary_components
- computed_amount: numeric(18,4) NOT NULL
- override_amount: numeric(18,4) nullable
- override_reason: text nullable
```

### Payslip Entity (MISSING in our build)
```
payslips
- id: uuid PK
- payrun_employee_id: uuid FK → payrun_employees (1:1)
- tenant_id
- generated_at: timestamptz
- file_key: text (MinIO S3 key for PDF)
- emailed_at: timestamptz nullable
- downloaded_count: int default 0
```

### BankAdviceFile (MISSING in our build)
```
bank_advice_files
- id: uuid PK
- payroll_run_id: uuid FK (1:1 per run)
- tenant_id
- generated_at: timestamptz
- file_key: text (MinIO S3 key)
- file_format: text (CSV/Excel — TBD)
```

## Zoho Employee Entity (Reference — 21 list columns reveal full attribute set)
- Employee Name, Work Email, Department, Employee Status (always visible)
- Cost to Company, DOB, DOJ, Designation, ESI Number
- Employee ID, Father Name, Gender, Last Working Day, Mobile Number
- Onboarding Status, PAN, PF A/C Number, Portal Status, Prior Payroll Status, UAN, Work Location
- **No Aadhaar field in Zoho** — notable absence vs some Indian HR systems

## Salary Components in Zoho (Settings > Salary Components)
### Earnings (14 configured)
| Component | Earning Type | Calc Basis | EPF | ESI | Status |
|---|---|---|---|---|---|
| Basic | Basic | 50% of CTC (default, user-editable) | Yes | Yes | Active |
| House Rent Allowance | HRA | 50% of Basic | No | Yes | Active |
| Conveyance Allowance | Conveyance | Flat Amount | Yes (if PF wage < 15k) | No | Active |
| Children Education Allowance | CEA | Flat Amount | Yes (if < 15k) | Yes | Inactive |
| Transport Allowance | Transport | Flat 1600 | Yes (if < 15k) | Yes | Inactive |
| Travelling Allowance | Travelling | Flat Amount | Yes (if < 15k) | No | Inactive |
| Fixed Allowance | Fixed | Flat Amount (residual) | Yes (if < 15k) | Yes | Active |
| Overtime Allowance | Overtime | Flat Amount | No | Yes | Inactive |
| Gratuity | Gratuity | Flat Amount | No | No | Active |
| Bonus | Bonus | Flat Amount | No | No | Active |
| Commission | Commission | Flat Amount | No | Yes | Active |
| Leave Encashment | Leave Encashment | Flat Amount | No | No | Active |
| Notice Pay | Notice Pay | Flat Amount | No | No | Active |
| Hold Salary | Hold Salary (Non Taxable) | Flat Amount | No | No | Active |

### Deductions (2)
- Withheld Salary (One Time, Active)
- Notice Pay Deduction (One Time, Active)

### Benefits (1)
- Voluntary Provident Fund (Recurring, Inactive)

### Reimbursements (5)
- Fuel, Driver, Vehicle Maintenance, Telephone, Leave Travel Allowance — all Inactive, max = 0
