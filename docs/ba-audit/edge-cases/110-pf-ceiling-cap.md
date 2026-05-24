# Edge Case > PF Ceiling Cap — EMP003

## Scenario
EMP003 (Vikram Nair) has a high salary. Verify PF is capped at ₹15,000 basic (statutory PF wage ceiling), not computed on actual basic salary.

## Steps to Reproduce
1. Navigate to Settings > Statutory Components > EPF
2. Check EPF configuration for wage ceiling
3. Navigate to EMP003 May 2026 payslip
4. Verify PF deduction = 12% × ₹15,000 = ₹1,800 (not 12% × actual basic)

## Expected Behaviour (statutory rule)
Per EPF Act and Employees' Provident Funds Scheme 1952:
- **PF wage ceiling**: ₹15,000/month (as notified; last updated in 2014)
- **Employee contribution**: 12% of PF wages (max ₹1,800/month if capped)
- **Employer contribution split**:
  - EPS (Employee Pension Scheme): 8.33% of PF wages (max ₹1,250/month if on ₹15,000 ceiling)
  - EPF (Employer PF): 3.67% of PF wages
- **EDLI (Employees' Deposit Linked Insurance)**: 0.50% of PF wages
- **PF Admin Charges**: 0.50% of PF wages (min ₹500/month)

For salary above ₹15,000, employer can choose:
- **Restricted PF**: Contribute only on ₹15,000 (statutory minimum)
- **Actual PF**: Contribute on actual basic salary (voluntary higher PF)

## Actual Zoho Behaviour

### EPF Configuration Status
From `/api/v1/components/statutorycompliance/epf`:
```json
{
  "is_active": false,
  "name": "Employee Provident Fund",
  "epf_employee_contribution": "12.00%",
  "eps_employer_contribution": "8.33%",
  "epf_employer_contribution": "3.67%",
  "edli_employer_contribution": "0.50%",
  "epf_admin_charges_employer_contribution": "0.50%",
  "deduction_cycle": "monthly",
  "is_employee_restricted_basic_enabled": false,
  "is_employer_restricted_basic_enabled": false,
  "consider_earned_salary_for_epf": true,
  "can_pro_rate_restricted_basic": false,
  "eps_senior_category_age": 58
}
```

**Critical finding: `is_active: false`**

EPF is **NOT enabled** for this organisation. The configuration is pre-populated with standard rates, but EPF is not active.

### Settings UI State
From the Settings > Statutory Components > EPF page:
- **Message**: "Are you registered for EPF? Any organisation with 20 or more employees must register for the Employee Provident Fund (EPF) scheme, a retirement benefit plan for all salaried employees."
- **Action**: "Enable EPF" link
- EPF registration number: blank
- **Status**: Not enrolled

### EMP003 Payslip State
EMP003 (Vikram Nair) was **skipped** in the May 2026 pay run:
```json
{ "payment_status": "skipped", "notes": "Onboarding incomplete" }
```
EMP003 cannot be processed — no payslip generated.

### PF Ceiling Configuration Fields (when EPF is enabled)
From the EPF config, Zoho supports these configurations relevant to PF wage ceiling:
- `is_employee_restricted_basic_enabled`: Restricts employee PF contribution to ₹15,000 wage ceiling
- `is_employer_restricted_basic_enabled`: Restricts employer PF contribution to ₹15,000 wage ceiling
- `can_override_restricted_basic`: Allows employee-level override of the restriction
- `can_pro_rate_restricted_basic`: Whether to pro-rate restricted basic for mid-month joiners
- `consider_earned_salary_for_epf`: If true, PF is computed on earned salary (after LOP deduction), not CTC salary

**Currently both `is_employee_restricted_basic_enabled` and `is_employer_restricted_basic_enabled` are false** — meaning if EPF were enabled, PF would be computed on the FULL basic salary, not capped at ₹15,000. This is the "voluntary higher PF" mode.

## Screenshots
- `screenshots/110-epf-not-enabled.png` — EPF settings page showing "not registered" state

## Gap / Bug / Surprise
1. **BLOCKER #1: EPF not enabled** — The entire PF ceiling test cannot be performed as EPF is not configured for this org.
2. **BLOCKER #2: EMP003 onboarding incomplete** — Even if EPF were enabled, EMP003 was skipped from the payrun.
3. **PF wage ceiling NOT enforced by default** — When EPF is enabled, both `is_employee_restricted_basic_enabled` and `is_employer_restricted_basic_enabled` are `false` by default. This means Zoho would compute PF on FULL basic salary unless the admin explicitly enables the restriction. This is **incorrect default behaviour** for most Indian orgs where employees earn above ₹15,000 — the statutory requirement is to contribute at minimum on ₹15,000, but Zoho defaults to full salary contribution.
4. **The ₹15,000 ceiling is a statutory MINIMUM definition**, not a maximum cap. Employers may contribute on higher wages voluntarily. Zoho's terminology of "restricted basic" is apt.
5. **`consider_earned_salary_for_epf: true`** — this is correct; PF should be on earned salary (after LOP), not CTC.
6. **EDLI and admin charges**: Correctly pre-configured at 0.50% each. Many payroll systems miss EDLI.

## How We Should Build This
- `ProvidentFundConfig` entity (org-level, not per-employee):
  - `is_active` (bool)
  - `registration_number` (string)
  - `registration_date` (date)
  - `pf_wage_ceiling` (decimal, default ₹15,000) — store as DB config, not hardcoded
  - `employee_contribution_rate` (decimal, default 12%)
  - `employer_epf_rate` (decimal, default 3.67%)
  - `employer_eps_rate` (decimal, default 8.33%)
  - `edli_rate` (decimal, default 0.50%)
  - `admin_charges_rate` (decimal, default 0.50%)
  - `restrict_pf_wage_to_ceiling` (bool, default TRUE — correct statutory default)
  - `consider_earned_salary` (bool, default true)
  - `deduction_cycle` (enum: monthly)
- Per-employee override: `EmployeePFConfig` can override to use actual basic (voluntary higher PF)
- PF computation:
  ```
  pf_wage = min(earned_basic, pf_wage_ceiling) if restrict_pf_wage_to_ceiling else earned_basic
  employee_pf = pf_wage × employee_contribution_rate
  employer_eps = min(pf_wage, 15000) × employer_eps_rate  // EPS always capped at 15000
  employer_epf = pf_wage × employer_epf_rate
  edli = pf_wage × edli_rate
  admin_charges = max(pf_wage × admin_charges_rate, 500)  // Minimum ₹500
  ```
- ECR file generation: Requires UNIVERSAL Account Number (UAN) per employee — add UAN field to Employee statutory info
