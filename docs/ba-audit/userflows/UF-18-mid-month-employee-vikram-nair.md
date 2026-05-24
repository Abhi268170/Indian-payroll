# UF-18: Mid-Month Employee — Vikram Nair (EMP003)

**Module:** Employees
**Tested:** 2026-05-16
**Mock Data Used:** Vikram Nair EMP003 (ID: 3848927000000034014)
**App State Before:** May 2026 pay run PAID; Vikram SKIPPED with "Onboarding incomplete"

## Steps Executed
1. Navigate to employee list — confirmed Vikram shows "This employee's profile is incomplete. Complete now"
2. Navigate to Vikram's Overview: `#/people/employees/3848927000000034014`
3. Navigate to Vikram's Salary Details: `#/people/employees/3848927000000034014/salary-details`

## Employee Profile Summary — Vikram Nair

| Field | Value |
|-------|-------|
| Employee ID | EMP003 |
| Name | Vikram Nair |
| Status | Active |
| Designation | Engineering Manager |
| Department | Engineering |
| Work Location | Head Office |
| Date of Joining | 01/01/2025 |
| Gender | Male |
| Email | vikram.nair@lerno.com |
| Mobile | 9876543212 |
| Portal Access | Disabled |

## What is MISSING (Causing "Onboarding Incomplete")

From the Personal Information section, all fields show "-":

| Missing Field | Value |
|---------------|-------|
| Date of Birth | "-" (not filled) |
| Father's Name | "-" (not filled) |
| PAN | "-" (not filled) |
| Personal Email Address | "-" (not filled) |
| Residential Address | "-" (not filled) |

The "Complete now" link points to: `#/people/employees/3848927000000034014/edit/personal-details`

**Conclusion: The "Onboarding Incomplete" gate is triggered by missing PAN and/or personal information fields, not by missing salary structure.**

## Salary Structure — IS Assigned

Vikram's salary structure IS configured despite being in "incomplete" status:

| Component | Monthly | Annual |
|-----------|---------|--------|
| Basic (50% of CTC) | ₹75,000.00 | ₹9,00,000.00 |
| Fixed Allowance | ₹75,000.00 | ₹9,00,000.00 |
| **Cost to Company** | **₹1,50,000.00** | **₹18,00,000.00** |

No HRA component — only Basic + Fixed Allowance. Salary structure IS present but employee is still blocked from payroll.

## Statutory Settings

| Statutory | Status |
|-----------|--------|
| EPF | Disabled |
| ESI | Disabled |
| Professional Tax | Enabled |
| Labour Welfare Fund | Disabled |

## Why Vikram is SKIPPED — Root Cause Analysis

The system requires specific Personal Information fields (primarily **PAN**) to be completed before an employee can be included in a pay run. This is a statutory requirement — TDS computation requires PAN. Without PAN:
- Form 16 cannot be generated
- TDS cannot be deducted correctly (default 20% rate applies per Income Tax Act Sec 206AA if PAN missing — but Zoho blocks payroll entirely instead)

**Onboarding Incomplete = Personal Information (specifically PAN) is missing**

Salary structure presence does NOT unblock the payroll gate — PAN (and likely DOB for age-based computation) must be filled.

## Proration Preview

No proration preview is shown on Vikram's profile or on the pay run page. The system simply shows "Skipped — Reason: Onboarding incomplete" with no calculation preview.

**Vikram's DOJ is 01/01/2025** — he is NOT a mid-month joiner for May 2026. He has been with the company since January 2025 but has been skipped every month due to incomplete profile.

## Navigation Available on Vikram's Profile

Same tab structure as active employees:
- Overview | Salary Details | Investments | Payslips & Forms | Loans

No "Complete Onboarding" wizard or step-by-step guide within the profile — only the banner message "This employee's profile is incomplete. Complete now" linking to personal details edit.

## Business Rules
- An employee with a salary structure assigned BUT missing PAN/personal information is included in the employee count (5 employees) but excluded from payroll processing
- The pay run shows them as "Skipped" with the reason "Onboarding incomplete"
- No partial payroll is computed — they are fully excluded, not partially processed
- There is no grace period or override mechanism visible to include a PAN-less employee

## Gaps / Observations
- 🔴 No proration calculator or preview for mid-month joiners visible anywhere in the UI
- The error message "Onboarding incomplete" is generic — it doesn't tell the admin exactly WHICH field is missing (just links to personal details page)
- Vikram has been an employee since Jan 2025 — over 16 months without being paid. No system alert or escalation visible for long-blocked employees.
- No "force include" option for admin to override the incomplete status for a one-off pay run
