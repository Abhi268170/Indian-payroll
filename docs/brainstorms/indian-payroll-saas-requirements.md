---
date: 2026-05-13
topic: indian-payroll-saas
---

# Indian Payroll SaaS — v1 Requirements

## Problem Frame

Indian companies running payroll deal with frequent statutory changes (PF, ESI, TDS slabs, PT state-by-state rules) and need a compliant, performant tool to run payroll for tens of thousands of employees. Existing SaaS options (Zoho Payroll, Greythr) work but are opaque, per-seat expensive, and non-customisable. This product is a multi-tenant, self-hosted Indian payroll SaaS — built on .NET + React — where each client organisation gets full statutory compliance, per-tenant configuration, and a clean employee self-service experience. Reference product: Zoho Payroll. V1 targets the new income tax regime exclusively; old regime support is deferred.

---

## Actors

- A1. **Platform Super Admin** — provisions/suspends tenant organisations, views cross-tenant usage metrics.
- A2. **Org Admin** — owns the tenant account, manages billing seat, sets company-level config.
- A3. **HR Manager** — manages employee master, org structure, onboarding/exit workflows, bulk employee imports.
- A4. **Payroll/Finance Manager** — runs monthly payroll cycle, reviews and finalises pay register, generates statutory challans and returns.
- A5. **Employee** — views and downloads own payslips and Form 16 via self-service portal.
- A6. **Background Worker** — async process that executes payroll batch computation and report generation.

---

## Key Flows

- F1. **Monthly Payroll Run**
  - **Trigger:** A4 initiates payroll run for a pay period (month + year).
  - **Actors:** A4, A6
  - **Steps:** (1) A4 uploads variable inputs file (CSV/Excel) containing per-employee LOP days and any ad-hoc earning/deduction components (food coupons, bus allowance, bonuses, deductions, etc.) for the period. (2) System validates the file — flags unknown component names, missing employee IDs, and format errors; A4 corrects and re-uploads if needed. (3) A4 triggers payroll run. (4) A6 computes gross (fixed components + variable inputs − LOP), all statutory deductions (TDS, PF, ESI, PT, LWF), employer contributions, and net pay for all active employees asynchronously. (5) A4 reviews draft pay register; can override individual employee figures or reprocess specific employees. (6) A4 finalises payroll — state transitions to Finalised and becomes immutable. (7) System generates payslips, challan data files, pay register exports, TDS working sheet (all employees on one sheet: name, PAN, monthly gross, YTD gross, projected annual income, standard deduction, taxable income, tax computed, TDS deducted YTD, balance TDS). Individual employee full payroll detail is also exportable per employee from the pay register view.
  - **Outcome:** Payroll finalised; payslips accessible to all employees via self-service; challan files and pay register downloadable by A4.
  - **Escape path:** If A6 processing fails for any employees, A4 sees per-employee error details and can correct data and reprocess those employees without re-running the full batch.
  - **Covered by:** R11, R12, R13, R14, R15, R16, R17, R40, R41, R44, R53

- F2. **Employee Onboarding & CTC Setup**
  - **Trigger:** A3 adds a new employee (single form or bulk import).
  - **Actors:** A3, A4
  - **Steps:** (1) A3 creates employee record with personal, employment, and statutory IDs. (2) A4 enters the employee's CTC; system generates a personal salary structure instance pre-populated with the tenant's default component formulas (Basic=40% of CTC, HRA=50% of Basic, etc.). (3) A4 reviews and customises any component for this specific employee — overriding formula, amount, or enabling/disabling components — without affecting any other employee's structure. (4) System auto-determines PF eligibility (Basic > ₹15,000 = opt-out allowed), ESI eligibility (gross ≤ ₹21,000), and PT state from work location. (5) A4 confirms statutory elections (PF opt-out if applicable). (6) Employee becomes active for the next regular payroll cycle.
  - **Outcome:** Employee enrolled with a personal salary structure and correct statutory flags; modifications to this employee's structure never affect other employees.
  - **Covered by:** R5, R6, R8, R9, R25, R27, R29

- F3. **Employee Exit / Full & Final Settlement**
  - **Trigger:** A3 marks employee as resigned/terminated with last working day.
  - **Actors:** A3, A4
  - **Steps:** (1) System computes prorated salary for the partial last month (days worked / working days in month × monthly gross). (2) A4 enters leave encashment days — system shows per-day rate (Basic / 26) and computes amount. (3) System auto-computes gratuity if tenure ≥ 5 completed years (15/26 × last Basic × completed years). (4) A4 enters notice period details: contracted notice period (months) vs. actual notice served — system computes notice pay recovery (if employee left early) or payment in lieu of notice (if company released early). (5) System fetches any outstanding loan balance from the loan module and includes it as a recovery deduction. (6) System computes TDS on total FnF payout under new regime — if FnF falls in a mid-year month, TDS shortfall from remaining projected months is collected here in full. (7) A4 reviews full FnF statement: prorated salary + encashment + gratuity + notice pay adjustment − loan recovery − TDS = net payable. (8) A4 can override any line item with an audit-logged reason. (9) A4 finalises; FnF payslip generated. (10) Employee set Inactive; excluded from all future payroll runs.
  - **Outcome:** FnF payslip generated and accessible to employee via self-service; loan balance closed; employee deactivated.
  - **Escape path:** All overrides (gratuity, encashment days, notice adjustment) are audit-logged with mandatory reason.
  - **Covered by:** R7, R18, R44, R61, R62

- F5. **Bulk Imports (across modules)**
  - **Trigger:** A3 or A4 initiates a bulk import from any module that supports it.
  - **Actors:** A3, A4
  - **Supported bulk imports:**
    - *Employees* — create or update employee master records (name, PAN, joining date, bank details, designation, etc.)
    - *Salary structure assignment* — CSV mapping Employee ID → CTC, with optional per-component overrides (e.g., `Basic_override=45000`)
    - *Variable payroll inputs* — per-run file with LOP days and ad-hoc components (already part of F1)
    - *YTD data* — per-employee historical figures for mid-year onboarding
    - *Loan data* — bulk create employee loans (employee ID, principal, EMI, start date)
  - **Common validation pattern for all imports:** (1) User downloads module-specific template. (2) User fills and uploads. (3) System validates per row and returns validation report (N valid, M errors with row number + message). (4) User corrects and re-uploads, or imports valid rows only. (5) All imports audit-logged.
  - **Outcome:** Data created/updated in bulk; errors surfaced per row; audit trail maintained.
  - **Escape path:** Partial import always allowed; failed rows do not block successful rows.
  - **Covered by:** R6, R58, R59, R61, R63

- F6. **Bulk Data Export**
  - **Trigger:** A4 or Finance Viewer requests an export from any finalised payroll period.
  - **Actors:** A4
  - **Steps:** (1) A4 selects export type from: pay register (Excel/PDF), individual employee payroll detail (single-employee export from pay register view), payslip bulk ZIP, TDS working sheet (all employees — one row per employee: name, PAN, monthly gross, YTD gross, projected annual income, standard deduction, taxable income, tax computed, TDS deducted YTD, balance TDS remaining), PF ECR file, ESI challan file, PT register, Form 24Q data, Form 16 bulk ZIP. (2) For large exports (>1,000 employees), system queues asynchronously and notifies A4 with download link. (3) For small exports, inline download. (4) All export events audit-logged.
  - **Outcome:** File downloaded; export event recorded in audit trail.
  - **Covered by:** R44, R46, R47, R54, R64

- F4. **Annual TDS Reconciliation & Form 16 Generation**
  - **Trigger:** A4 initiates year-end close after March payroll is finalised.
  - **Actors:** A4, A6
  - **Steps:** (1) System computes each employee's actual annual income (sum of all 12 months' gross) and the exact tax owed on it under new regime slabs + cess. (2) System compares total tax owed vs. total TDS deducted across April–February; any shortfall is shown per employee. (3) March payroll automatically collects the full shortfall in March TDS so the annual numbers balance. (4) A4 reviews the reconciliation summary (all employees, expected tax vs. deducted vs. March adjustment) and approves. (5) System generates Form 24Q Q4 data file in FVU/text format — A4 submits to TRACES/TIN-NSDL externally and downloads Form 16 Part A from TRACES after acceptance. (6) A4 uploads the TRACES-generated Part A data into the system; A6 merges Part A with system-generated Part B (salary breakdown, standard deduction, taxable income, tax computation) per employee. (7) Form 16 published to each employee's self-service portal; bulk ZIP downloadable by A4.
  - **Outcome:** Annual TDS balanced; Form 16 Part A + B available per employee; Form 24Q Q4 data exported.
  - **Escape path:** If employee left mid-year (FnF), their TDS reconciliation was completed at FnF; they are included in Form 24Q but Form 16 is generated as of their last working month.
  - **Covered by:** R20, R21, R22, R23, R24, R36, R37

- F7. **Off-Cycle Payroll Run (Bonus / Incentive)**
  - **Trigger:** A4 initiates an off-cycle run for specific employees outside the regular monthly cycle.
  - **Actors:** A4, A6
  - **Steps:** (1) A4 selects employees to include in the off-cycle run (individual or filtered list). (2) A4 uploads or enters per-employee payout amounts and component names (e.g., Performance Bonus ₹50,000, Retention Incentive ₹20,000). (3) System computes TDS on off-cycle payouts — added to the employee's YTD income for annualisation in the current month's TDS projection. (4) PF and ESI applicability on off-cycle components determined by component configuration (most bonus types are non-PF, non-ESI — but configurable). (5) A4 reviews computed TDS and net payouts. (6) A4 finalises; off-cycle payslips generated separately (not merged with the regular monthly payslip). (7) Off-cycle YTD figures feed into TDS working sheet and Form 24Q.
  - **Outcome:** Off-cycle payslips generated; TDS correctly updated in employee's YTD; no disruption to regular monthly payroll cycle.
  - **Covered by:** R11, R15, R17, R65

---

## Requirements

**Configuration & Customisation**

- R0. All tenant-level customisations (salary components, pay grades, PT slabs, LWF rules, tax slabs, variable input component names) are stored as configuration data in each tenant's schema. No per-tenant code changes or separate deployments are used to deliver customisation. One running application instance serves all tenants.
- R0a. Each statutory module is independently togglable by Org Admin (A2) at the tenant level: PF (entire module on/off), ESI (entire module on/off), PT (per state on/off), LWF (per state on/off), VPF (option available to employees on/off). Disabling a module excludes it from all payroll computations, payslips, and challan generation for that tenant. Enabling/disabling is audit-logged with effective date.

**Multi-tenancy & Platform**

- R1. Each tenant organisation is isolated in its own database schema; no query, join, or API call can return data across tenant boundaries.
- R2. A6 runs batch payroll jobs per-tenant in isolation; one tenant's compute load cannot block or delay another tenant's API or batch queue.
- R3. Platform super admin (A1) can provision new tenants, suspend tenants, and view per-tenant active employee count and last payroll run date.
- R4. Tenant schema migrations and backups are independently executable without affecting other tenants.

**Organisation & Employee Management**

- R5. Org structure is configurable per tenant: Company → Branches → Departments → Designations → Cost Centres, with employees mapped to any leaf node. Each level is optional (a company with no branches still works); names are free text. Employees must be assigned to at least a Department and a Designation.
- R6. Employee master captures: full name, date of birth, gender, personal address, work location (state — used for PT and LWF); designation, department, branch, cost centre, joining date, employment type (full-time / contract / probation); statutory IDs: PAN (mandatory — used for TDS; employees without PAN taxed at 20% per Section 206AA), Aadhaar (optional — missing triggers a warning, does not block payroll), UAN (for PF), ESI IP number; bank details: account number and IFSC; CTC and salary structure assignment. Each field has clear mandatory/optional/conditional labelling in the UI.
- R7. Exit (FnF) workflow: (a) system auto-computes prorated salary for the partial last month (days worked / total working days × monthly gross); (b) A4 manually enters leave encashment days — system displays the per-day rate (Basic / 26) and computes the amount; (c) system auto-computes gratuity if tenure ≥ 5 completed years (15/26 × last Basic × completed years); (d) system computes TDS on the total FnF payout under new regime; (e) A4 reviews and can override any computed amount with an audit-logged reason before finalising; (f) system produces a finalised FnF payslip PDF and deactivates the employee.
- R8. Tenant defines a master set of salary components at company level: earning heads (Basic, HRA, LTA, Special Allowance, unlimited custom names) and deduction heads (PF Employee, ESI Employee, PT, LWF, unlimited custom names). Each component has a default formula rule (fixed amount, % of Basic, % of CTC, % of Gross), a PF-eligible wages flag, and a TDS taxable flag. Pre-built statutory components have locked formula rules but can be toggled on/off per tenant. This master set is the template from which employee-level instances are created.
- R8a. Each employee has a personal salary structure instance — a copy of the tenant master at the time of onboarding. Any component formula, amount, or enabled/disabled state can be overridden per employee without affecting any other employee's structure or the tenant master. Example: Employee A's HRA = 50% of Basic (default); Employee B's HRA = fixed ₹15,000 (override). Both coexist independently.
- R8b. When the tenant adds a new salary component to the master, A4 can choose to push it to all existing employee instances (with a default value) or only to new employees going forward. Pushed changes to existing employees are audit-logged.
- R9. Tenant can define pay grades/bands (e.g., L1, L2, Senior Manager) with a CTC range per grade. When assigning a salary structure to an employee, the system suggests a default breakdown based on the employee's grade, which A4 can override. Grade assignment is optional — employees without a grade get manual CTC structuring.
- R10. Salary revision is effective-date-based: A4 enters new CTC and effective date; system computes arrears as (new monthly gross − old monthly gross) × number of months since effective date. Arrears are included as a named earning component in the next payroll run and taxed under TDS accordingly.

**Bulk Import & Export**

- R58. System provides a downloadable CSV/Excel template for employee bulk import with all fields, field descriptions, and allowed values documented in the template header rows. Template is versioned — changes to the employee master surface as a new template version with a changelog.
- R59. Bulk employee import validates each row independently; A3 sees a summary (N rows valid, M rows with errors) and a per-row error list before committing the import. Partial import (valid rows only) is allowed. Import is atomic per row — no partial employee record is created.
- R60. Pay register, payslip bulk ZIP, PF ECR, ESI challan, PT register, Form 24Q data, and Form 16 bulk ZIP are all exportable by A4 from any finalised payroll period. Exports for >1,000 employees are async with download link delivery; ≤1,000 employees are sync inline download.

**Core Payroll Engine**

- R11. Monthly payroll run computes, for every active employee: (a) gross salary = sum of all fixed earning components + variable earning inputs from the uploaded file − LOP deduction; (b) statutory deductions: TDS (annualised projection), PF employee, ESI employee (if applicable), PT (state-specific), LWF (state-specific, frequency-aware); (c) employer contributions: PF employer (EPF + EPS + EDLI + admin), ESI employer (if applicable); (d) custom deductions from variable inputs file; (e) net pay = gross − all deductions.
- R12. LOP deduction computed as: (monthly gross / working days in the calendar month) × LOP days from the variable inputs file. "Working days" uses the calendar month's total days minus Sundays by default; tenant can configure a fixed working-days-per-month value instead.
- R13. Arrears from salary revisions are included as a distinct earning line in the payroll period they are processed; the arrears amount is separately visible in the pay register and on the payslip, and is included in TDS projection for that month.
- R14. A payroll run is idempotent for a given period — re-running with the same variable inputs file and the same employee data produces bit-for-bit identical output. Any difference in output indicates a data change, which is audit-logged.
- R15. Payroll run states: **Draft** — computed, A4 can review, reprocess individuals, and re-trigger without restriction; **Finalised** — immutable, payslips generated, challan files available. Moving from Finalised back to Draft (to correct an error) requires an explicit unlock action by A2 or A4, recorded in the audit log with a mandatory reason.
- R16. Individual employee payroll can be recomputed within a Draft run — A4 selects specific employees, adjusts their inputs, and retriggers computation for those employees only. The rest of the pay register is unaffected.
- R17. Batch payroll for 10,000 employees executes asynchronously via background worker (A6) with a target SLA of 30 minutes end-to-end. A4 receives an in-app notification and email on completion (success) or failure. On failure, the notification includes which employees failed and the error reason.

**TDS — New Tax Regime Only**

- R18. TDS is computed exclusively under the new tax regime (Section 115BAC). Old regime computation is not in scope for v1.
- R19. Each month, TDS is projected by annualising year-to-date earnings plus projected remaining months' salary.
- R20. Standard deduction of ₹75,000 is applied automatically before tax computation.
- R21. Tax slabs are stored as configurable data (not hardcoded) so that Budget-driven slab changes require a data update, not a code deployment. Current new-regime slabs (FY 2025-26): ₹0–4L: 0%, ₹4–8L: 5%, ₹8–12L: 10%, ₹12–16L: 15%, ₹16–20L: 20%, ₹20–24L: 25%, >₹24L: 30%.
- R22. Surcharge applied on tax per income slab (new regime cap: 25%): ₹50L–₹1Cr → 10%, ₹1Cr–₹2Cr → 15%, >₹2Cr → 25%. Marginal relief computed at each boundary so net post-surcharge tax never exceeds the incremental income above the threshold. Health and Education Cess of 4% applied on (tax + surcharge). Surcharge thresholds and rates are configurable data, not hardcoded.
- R23. Section 89(1) relief for arrears salary is computed and applied where applicable.
- R24. TDS shortfall (where projected TDS in earlier months was lower than actual) is redistributed across remaining months in the financial year.

**Provident Fund (EPF/EPS/EDLI)**

- R25. Employee PF contribution: 12% of (Basic + DA). Employees may additionally elect a VPF (Voluntary Provident Fund) contribution — a fixed amount or percentage above 12%, recorded on the employee record, deducted monthly, and included in the ECR. VPF does not attract any employer matching contribution.
- R26. Employer PF contribution split per statute: 3.67% to EPF, 8.33% to EPS (capped ₹1,250/month), 0.50% to EDLI (capped ₹75/month), 0.50% EPF admin charge on PF-eligible wages (minimum ₹500/month at tenant level if any employee is covered; EDLI admin charge is NIL per Jan 2020 waiver). All rates configurable; not hardcoded.
- R27. Employees with Basic > ₹15,000/month may elect to opt out of PF at joining; election recorded on employee record and respected in all payroll runs. Existing PF members who cross the ₹15,000 threshold mid-employment continue contributing unless they explicitly opt out.
- R28. ECR (Electronic Challan cum Return) file generated per finalised payroll period in the EPFO UAN portal prescribed format, downloadable by A4.

**ESI**

- R29. ESI applicability per employee: gross salary ≤ ₹21,000/month (₹25,000 for persons with disability) triggers coverage from the employee's first day if they join at or below the threshold. New joiners whose gross exceeds the threshold at joining are not covered for that contribution period. Coverage is reviewed at each period boundary (April, October).
- R30. Employee ESI contribution: 0.75% of gross salary. Employer ESI contribution: 3.25% of gross salary. Gross for ESI includes all variable inputs (food coupons, bus allowance, ad-hoc earnings) from the variable inputs file.
- R31. An employee whose gross crosses ₹21,000 mid-contribution-period remains covered through end of that period (ESIC rules). Coverage stops from the next contribution period start if gross remains above threshold.
- R32. ESI challan data file generated per contribution period in prescribed ESIC portal format, downloadable by A4.

**Professional Tax**

- R33. PT deduction is driven by state-specific slab configuration stored as data (not hardcoded); slabs are updatable via admin configuration without code deployment. PT module is designed for multi-state addition in later phases.
- R34. V1 supports Kerala PT only. Kerala uses two contribution periods (April–September, October–March). Each month, the system checks the employee's cumulative gross earnings for the current period, looks up the applicable Kerala PT slab, and deducts (slab amount ÷ remaining months in period). Slab values are configurable data. Other states added in later phases.

**Labour Welfare Fund**

- R35. LWF deduction and employer contribution are configured per state, with support for monthly, half-yearly, and annual deduction frequencies as required by state law.

**Statutory Returns & Challans**

- R36. Form 24Q data (TDS quarterly return) generated per quarter per tenant in the prescribed FVU/text format for submission to TRACES/TIN-NSDL; covering all four quarters.
- R37. Form 16 Part A (TDS certificate from employer) and Part B (salary breakdown) generated per employee per financial year after annual TDS reconciliation is approved; downloadable by A5 via self-service.
- R38. PF ECR file generated per finalised monthly payroll run.
- R39. ESI challan data generated per contribution period (April–September, October–March).

**Variable Pay & Per-Run Inputs**

- R40. Each payroll run accepts a variable inputs file (CSV or Excel) uploaded by A4 before the run is triggered. The file contains one row per employee with: Employee ID, LOP days (can be 0), and any number of ad-hoc component columns (e.g., "Food Coupon," "Bus Allowance," "Performance Bonus," "Canteen Deduction"). Column headers must match configured earning or deduction component names exactly; unrecognised headers are flagged as errors before processing begins.
- R41. System validates the variable inputs file before accepting it: (a) all Employee IDs must exist and be active for that pay period; (b) LOP days must be a non-negative number not exceeding the working days in the month; (c) numeric values required for all component columns. Validation errors are returned as a row-by-row report — upload is rejected until errors are resolved or out-of-scope rows are removed.
- R42. Variable inputs file is stored per payroll run as an immutable audit artifact — the exact file uploaded, by whom, and when. Re-uploading replaces the previous file but the prior version is retained in the audit log.
- R43. If no variable inputs file is uploaded for a pay period, the system treats LOP as 0 and applies no ad-hoc components. Payroll run is not blocked by a missing file; A4 is shown a warning banner confirming no variable inputs were provided before triggering the run.

**Payslips & Reports**

- R44. Payslip generated per employee per finalised payroll run, showing all earning components, deduction components (statutory + voluntary), employer contributions (informational), and net pay; available as PDF.
- R45. Employees (A5) access their own payslips and Form 16 via a self-service portal secured by personal login; PDFs are optionally password-protected with PAN.
- R46. Pay register (all employees, all components, one row per employee) exportable as Excel and PDF from any finalised payroll run.
- R47. Statutory reports available: PF monthly statement, ESI monthly statement, PT register, challan summaries.

**Security**

- R48. Role-based access control with these roles at minimum: Super Admin (platform-level), Org Admin (tenant), HR Manager, Payroll Manager, Finance Viewer (read-only on pay register and reports), Employee (own data only). Org Admin inherits all permissions of every role below it within their tenant. A single user can hold multiple roles.
- R49. PAN, Aadhaar, and bank account numbers stored encrypted at rest (AES-256 or equivalent); decrypted only on authorised access.
- R50. All client-server communication over TLS 1.2+.
- R51. Immutable audit trail records every mutation to payroll data, salary structures, and employee statutory fields: actor, timestamp, field changed, old value, new value; audit trail is queryable by A2 and A1.
- R52. Aadhaar numbers displayed in UI as masked (last 4 digits only) per UIDAI guidelines; full number visible only to authorised roles with an audit log entry on each view.

**Loan Management**

- R61. A4 can create a loan record for any employee: loan amount (principal), EMI amount per month, start month, and an optional end date. Multiple loans per employee are supported.
- R62. Each payroll run automatically includes any active loan EMI as a deduction component in that employee's payslip. The deduction appears as a named line item ("Loan Recovery — [Loan Name]"). Remaining balance is updated after each deduction.
- R63. Loans are bulk-importable via CSV (Employee ID, loan name, principal, EMI, start month). Validation checks: employee must exist and be active; EMI must be ≤ principal; start month must be current or future.
- R64. On employee exit (FnF), the outstanding loan balance across all active loans is auto-fetched and shown as a recovery deduction in the FnF statement. A4 can override the recovery amount with an audit-logged reason (e.g., company waiver).
- R65. Loan dashboard per employee shows: original principal, total recovered to date, outstanding balance, projected closure month, and month-by-month recovery history.

**Off-Cycle & TDS Exports**

- R66. Off-cycle payroll run targets a subset of employees (selected individually or by filter); it is independent of the regular monthly run — it does not affect or re-trigger monthly payroll. Off-cycle runs follow the same Draft → Finalised state machine as regular runs.
- R67. Off-cycle payout components are configurable per run (e.g., Performance Bonus, Retention Bonus); each component is tagged taxable/non-taxable and PF-eligible/non-eligible. TDS on the off-cycle payout is computed by adding it to the employee's YTD income and recomputing the annualised projection for the current month.
- R68. Off-cycle payslips are generated and accessible in employee self-service separately from regular monthly payslips. Off-cycle amounts are included in the employee's YTD figures for Form 24Q and Form 16.
- R69. TDS working sheet exportable from any payroll period (regular or off-cycle): one row per employee, columns: Employee Name, PAN, Monthly Gross, YTD Gross, YTD Off-Cycle Payouts, Projected Annual Income, Standard Deduction, Taxable Income, Tax Computed, TDS Deducted YTD, Balance TDS (remaining months). Available as Excel.
- R70. Individual employee payroll detail exportable from the pay register view: shows all earning components, deduction components, employer contributions, YTD figures, and TDS computation for that employee for the selected period. Available as PDF and Excel.

**Mid-Year Onboarding**

- R56. When a tenant onboards mid-financial-year, HR can enter per-employee YTD figures: YTD gross salary, YTD TDS deducted, YTD PF employee contribution, and YTD ESI employee contribution for all prior months in the current FY. These figures are used in TDS projection (annualisation) and Form 16 generation for the full year.
- R57. YTD import data is audit-logged (entered by whom, when) and cannot be modified after the first payroll run that uses them without an explicit override by A2.

**Performance**

- R53. Payroll batch jobs execute asynchronously via a background worker queue; the payroll API remains responsive to all other requests during a batch run.
- R54. Report generation for tenants with >1,000 employees runs asynchronously; A4 receives a download link when generation is complete.
- R55. Per-tenant batch jobs are queued and resource-capped such that one tenant's large batch cannot starve other tenants' job queue slots.

---

## Acceptance Examples

- AE1. **Covers R1.** Given two tenants T1 and T2 exist, when any API call is made with T1's auth token, no T2 employee or payroll record is returned or accessible — including via ID enumeration.
- AE2. **Covers R14, R15.** Given payroll for April is in Draft state, when A4 runs payroll again without changing any inputs, the computed net pay for every employee is identical to the first run.
- AE3. **Covers R18, R21.** Given an employee with annual gross of ₹18L and standard deduction applied, when payroll is run, TDS is computed using new-regime slabs only; no old-regime deduction (80C, 80D, HRA exemption) is applied.
- AE4. **Covers R31.** Given an employee whose March gross is ₹20,800 (under ₹21,000), then April gross becomes ₹22,000, ESI continues to be deducted through September (end of contribution period) and stops from October.
- AE5. **Covers R52.** Given A3 views the employee detail page, Aadhaar is shown as `XXXX-XXXX-1234`; an audit log entry is created when an authorised admin role explicitly reveals the full number.
- AE6. **Covers R17, R53.** Given a tenant with 10,000 active employees, when A4 triggers payroll run at 10:00 AM, the payroll API responds to other requests normally throughout; A4 receives a completion notification by 10:30 AM.

---

## Success Criteria

- A Payroll Manager can onboard a new company, configure salary structures, load 500+ employees, and run the first payroll cycle end-to-end without contacting support.
- All statutory deductions (TDS, PF, ESI, PT, LWF) computed for a sample payroll match hand-calculated values using published statute rules for FY 2025-26.
- Form 16 generated by the system passes TRACES format validation and can be used by employees for ITR filing.
- No tenant can read another tenant's data in any surface (API, reports, exports) under any condition.
- A planning agent can produce a detailed implementation plan from this document without inventing product behaviour, scope boundaries, or compliance rules.

---

## Scope Boundaries

### Deferred for later

- Perquisite taxation (company car, rent-free accommodation, stock options, club memberships).
- PT for states other than Kerala (Maharashtra, Karnataka, West Bengal, Tamil Nadu, Gujarat, AP, Telangana, MP, Assam — added in later phases).
- Section 192(2B) — employee declaring other income sources (rental, interest, capital gains) for TDS computation.
- Old income tax regime (Form 12BB investment declarations, Section 80C/D/E/G deductions, HRA exemption calculation, old regime TDS computation).
- Automated e-filing of TDS returns to TRACES/TIN-NSDL; v1 generates compliant data files, filing is manual.
- Loan interest computation (v1 loans are interest-free; interest-bearing loans deferred).
- Expense reimbursement module.
- Mobile app (iOS/Android).
- Bulk automated migration of historical payroll data; v1 supports basic YTD manual entry (see R56).
- Integration with specific accounting tools (Tally, QuickBooks) beyond CSV export.

### Outside this product's identity

- General HRMS (performance management, appraisals, recruitment, org charts) — this is a payroll product, not a people platform.
- Payroll bureau/outsourcing workflow (accountants managing 50 clients from one dashboard) — different actor set and UX model.
- Multi-country payroll — Indian payroll only.
- ERP replacement — no GL posting engine, no accounts payable module.

---

## Tech Stack

### Backend
| Concern | Choice |
|---|---|
| Runtime | .NET 8 LTS (ASP.NET Core Web API) |
| Architecture | Clean Architecture + CQRS via MediatR |
| ORM | EF Core 8 (writes + migrations), Dapper (complex read queries) |
| Database | PostgreSQL 16 + PgBouncer (connection pooling) |
| Background jobs | Hangfire Core (PostgreSQL-backed, built-in dashboard) |
| Auth | OpenIddict + ASP.NET Identity (OAuth2/OIDC, MFA, token revocation) |
| Caching + locks | Redis (distributed cache, distributed lock for payroll run dedup) |
| Validation | FluentValidation (MediatR pipeline behaviours) |
| Observability | OpenTelemetry → Prometheus + Grafana |
| Logging | Serilog → Grafana Loki (structured, queryable) |
| Encryption | ASP.NET Data Protection + AES-256 column-level (PAN, Aadhaar, bank account) |
| Secrets | Docker secrets + environment injection (no secrets in code) |
| Error format | RFC 7807 Problem Details |

### Payroll Calculation Engine
Standalone class library — no I/O, no framework dependencies. Fully unit-testable. All monetary values use `decimal`. Modules: TDSCalculator, PFCalculator, ESICalculator, PTCalculator, LWFCalculator, GrossCalculator, FnFCalculator.

### Frontend
| Concern | Choice |
|---|---|
| Framework | React 18 + Vite + TypeScript (strict mode) |
| UI components | shadcn/ui + Tailwind CSS |
| Data tables | TanStack Table v8 (virtualised, 10k-row capable) |
| Server state | TanStack Query v5 |
| Forms | React Hook Form + Zod |
| Global state | Zustand |
| HTTP | Axios (auth interceptor for JWT injection) |

### Infrastructure (Docker Compose, Linux VPS)
| Container | Purpose |
|---|---|
| nginx | SSL termination, security headers (HSTS, CSP, X-Frame-Options) |
| api | ASP.NET Core 8 |
| worker | Hangfire worker (separate container, same image) |
| db | PostgreSQL 16 |
| pgbouncer | Connection pooler |
| redis | Cache + distributed locks |
| minio | Object storage (payslips, exports, templates) |
| prometheus | Metrics |
| grafana | Dashboards + Loki log viewer |

**All components free/open source. VPS is the only cost (~₹1,500–3,000/month on Hetzner).**

### Application Architecture

**Pattern:** Single deployable monolith — Clean Architecture + CQRS (MediatR)

```
API Layer         Controllers → MediatR pipeline (auth, validation, tenant, logging)
Application       Commands (RunPayroll, FinalisePayroll, CreateEmployee)
                  Queries  (GetPayRegister, GetTDSWorksheet)
Domain            PayrollEngine class library — pure C#, no I/O, no frameworks
Infrastructure    EF Core (writes), Dapper (reads), PostgreSQL, Redis, MinIO, Hangfire
```

**Tenant resolution:** Subdomain + JWT double-lock. Nginx extracts subdomain → API middleware resolves `tenant_id` from Redis-cached registry → validates JWT `tenant_id` matches subdomain → sets scoped `TenantContext`. Mismatched token = 403.

**Multi-tenancy DB:** `TenantDbContextFactory` creates a scoped `PayrollDbContext` per request with `HasDefaultSchema(tenantId)`. All EF queries are schema-bound at construction. PgBouncer pools connections across all tenant schemas. Migration runner iterates all tenant schemas on deploy.

**Payroll engine:** In-process class library called by Hangfire worker. Pure functions, `decimal` arithmetic, zero I/O. Hangfire job: load inputs from DB → call `PayrollEngine.Compute()` → save results → queue payslip jobs. Redis distributed lock prevents duplicate runs per tenant. Scales horizontally by adding worker containers — Hangfire distributes jobs automatically.

### Security non-negotiables
- Column-level AES-256 encryption: PAN, Aadhaar, bank account (application layer, not just TDE)
- PostgreSQL RLS as second isolation layer under schema-per-tenant
- All endpoints require JWT + tenant claim middleware
- Rate limiting on auth endpoints
- Audit trail: append-only PostgreSQL table, no UPDATE/DELETE (enforced via trigger)
- Payslip PDFs password-protected with employee PAN
- PAN/Aadhaar masked in all application logs before Serilog sinks

---

## Key Decisions

- **New tax regime only for v1:** Reduces compliance surface significantly. Old regime requires collecting and verifying investment proofs, a separate declaration workflow, and dual computation paths. Deferred deliberately; most new employees default to new regime already.
- **Aadhaar optional, warn only:** Aadhaar is required for PF UAN seeding but not for payroll computation itself. System warns but does not block; operationally practical for companies with older or contract employees.
- **Mid-year YTD import in v1:** Companies onboarding mid-year need prior-months' YTD figures for accurate TDS annualisation and Form 16. Manual per-employee entry chosen over automated import to keep scope bounded.
- **Employee-centric salary structure instances:** Each employee gets a personal copy of the tenant's component master at onboarding. Individual overrides never affect other employees. Tenant master serves as default template only, not a locked shared structure.
- **Loan management in v1 (interest-free):** Company loans with EMI recovery built into monthly payroll and FnF. Interest-bearing loans deferred.
- **Off-cycle runs for bonus/incentive:** Separate from monthly payroll cycle, targeted at specific employees. TDS impact flows into YTD correctly.
- **Schema-per-tenant multi-tenancy:** Chosen over row-level isolation for data safety (no cross-tenant data leakage possible at DB query level) and compliance auditability. Chosen over DB-per-tenant to keep operational complexity manageable at scale. Shardable to multiple PostgreSQL instances by adding `ConnectionString` to tenant registry — no application code changes.
- **Async batch payroll with job queue:** Synchronous payroll run for 10k employees would exceed API timeout limits. All large compute runs are async with completion notification. Scales horizontally by adding Hangfire worker containers.
- **Configurable slabs and PT tables (data-driven, not code):** Budget changes and state PT amendments are annual events; requiring a code deployment for each slab change is fragile. Slabs are config data, updatable by super admin.
- **In-process payroll engine:** Pure C# library, no I/O. Called by Hangfire worker. Calculation of 10k employees takes seconds of CPU — the 30-min SLA is dominated by DB I/O and PDF generation, not arithmetic. Extractable to a separate service if ever needed; interface does not change.
- **Subdomain + JWT tenant resolution:** Subdomain identifies tenant at login; JWT carries tenant_id on every request. Middleware validates both match — prevents token reuse across tenants. Stateless, horizontally scalable.

---

## Dependencies / Assumptions

- The platform operator (developer/owner) manages hosting infrastructure; clients access via browser only. No on-premise client deployment in v1.
- Financial year follows April–March (Indian FY). The system is FY-aware for TDS projection and Form 16 generation.
- PAN is mandatory for all employees for TDS computation; employees without PAN are deducted TDS at 20% (Section 206AA).
- Gratuity is computed per Payment of Gratuity Act 1972: 15/26 × last drawn Basic × completed years of service, triggered on exit for tenure ≥ 5 years.
- Surcharge thresholds and cess rate assumed to be data-configurable (same principle as tax slabs).
- v1 does not include a pension management or NPS contribution module beyond standard PF.

---

## Outstanding Questions

### Resolve Before Planning

*(none — all blocking questions resolved)*

### Deferred to Planning

- **[Affects R28, R36][Needs research]** Confirm current EPFO ECR v2 file format spec and TRACES Form 24Q FVU format before schema design — both have periodic minor revisions.
- **[Affects R34][Needs research]** Confirm current Kerala PT slab values for active FY before implementing PTCalculator.
- **[Affects R35][Needs research]** Confirm Kerala LWF rates and deduction date rules (annual December deduction, employment-on-rolls check) before implementing LWFCalculator. Determine if LWF scope matches PT scope (Kerala-only v1).
- **[Affects R37][Technical]** Form 12BA — confirm not required since perquisites are deferred. Revisit when perquisites are added.
- **[Affects R18–R24][Technical]** Detailed TDS engine design: annualisation formula, shortfall redistribution algorithm, Section 89(1) relief computation, marginal relief on surcharge — discuss at build time.
- **[Affects R25–R26][Technical]** PF engine: ECR file column mapping, EDLI cap enforcement, EPF admin charge minimum logic — confirm against latest EPFO spec at build time.
- **[Affects R29–R31][Technical]** ESI engine: contribution period boundary logic, new joiner mid-period coverage rules — confirm against latest ESIC circular at build time.

---

## Next Steps

-> `/ce-plan` for structured implementation planning.
