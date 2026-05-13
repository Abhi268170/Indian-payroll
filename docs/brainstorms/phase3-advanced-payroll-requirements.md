---
date: 2026-05-13
topic: indian-payroll-phase3-advanced-payroll
master: docs/brainstorms/indian-payroll-saas-requirements.md
---

# Phase 3 — Advanced Payroll

## Goal

Full payroll lifecycle: salary revision with arrears, Full & Final Settlement, off-cycle runs, annual TDS reconciliation, Form 16, Form 24Q (all 4 quarters), mid-year YTD onboarding. After Phase 3 the system handles every payroll event a company encounters across a full financial year.

**Prerequisite:** Phase 2 complete and deployed.

---

## Actors

- A3. HR Manager — marks employee exits, initiates FnF
- A4. Payroll/Finance Manager — salary revisions, off-cycle runs, FnF finalisation, TDS reconciliation
- A5. Employee — downloads Form 16 from self-service
- A6. Background Worker — Form 16 batch generation, off-cycle computation

---

## Key Flows (Phase 3 scope)

- **F3 (complete):** Employee Exit / FnF — prorated salary, leave encashment, gratuity, notice pay, loan recovery (scaffolded from Phase 4; A4 manually enters if loans not yet live), TDS on FnF, finalise, deactivate employee.
- **F4 (complete):** Annual TDS reconciliation — reconcile projected vs. actual TDS, March adjustment, approve, generate Form 24Q Q4, upload TRACES Part A, generate Form 16 Part A+B, publish to employee self-service.
- **F7 (complete):** Off-cycle payroll run — select employees, enter payout amounts, compute TDS impact on YTD, finalise, generate off-cycle payslips.

---

## Requirements

**Salary Revision & Arrears**
- R10. Salary revision: effective-date-based. System computes arrears = (new monthly gross − old monthly gross) × months since effective date. Arrears appear as named earning line in next payroll run, included in TDS projection.
- R13. Arrears line separately visible in pay register and payslip.
- R23. Section 89(1) relief: when arrears relate to a prior year, relief computed and applied to reduce TDS on the arrears amount.

**FnF — Full & Final Settlement**
- R7. FnF workflow: (a) prorated salary for partial last month (days worked / working days × monthly gross); (b) A4 enters leave encashment days — system shows daily rate (Basic/26), computes amount; (c) gratuity auto-computed if tenure ≥ 5 completed years (15/26 × last Basic × completed years); (d) notice pay: contracted vs. served months — system computes recovery or payment-in-lieu; (e) outstanding loan balance auto-fetched (if Phase 4 loans live) or manually entered; (f) TDS on total FnF under new regime — full shortfall collected in FnF month; (g) A4 reviews, can override any line with audit-logged reason; (h) finalise → FnF payslip → employee Inactive.

**Off-Cycle Runs**
- R66. Off-cycle run: subset of employees. Independent of monthly run. Same Draft → Finalised state machine.
- R67. Off-cycle components configurable per run (taxable/non-taxable, PF-eligible flags). TDS recomputed by adding payout to YTD income for annualisation.
- R68. Off-cycle payslips accessible in employee self-service separately. Off-cycle amounts included in YTD for Form 24Q and Form 16.

**Annual TDS Reconciliation & Form 16**
- R24. TDS shortfall redistribution fully operational (was partially live in Phase 2; March collection finalised here).
- R36 (complete). Form 24Q all 4 quarters in FVU/text format.
- R37. Form 16: Part A (A4 uploads TRACES-downloaded Part A), Part B (system-generated salary breakdown, standard deduction, taxable income, tax computation). System merges. Published to employee self-service. Bulk ZIP downloadable by A4.

**Mid-Year YTD Onboarding**
- R56. Per-employee YTD entry: YTD gross, YTD TDS deducted, YTD PF employee, YTD ESI employee for months before onboarding. Used in TDS annualisation and Form 16.
- R57. YTD data audit-logged. Locked after first payroll run that uses them; override requires A2 with reason.

---

## Out of Scope (Phase 3)

- Loan EMI deductions (Phase 4 — FnF in Phase 3 accepts manual loan recovery input)
- Bulk async exports >1,000 employees (Phase 4)
- Super admin dashboard metrics (Phase 4)
- PT/LWF for states beyond Kerala (future)

---

## Success Criteria

- Salary revision with effective date 2 months prior produces correct arrears in the next payroll run; arrears taxed correctly under new regime.
- FnF for an employee with 6 years tenure produces correct gratuity; TDS on FnF matches hand-calculation.
- Off-cycle bonus run for 5 employees produces separate payslips; YTD TDS figures in the next monthly run reflect the off-cycle payout.
- Form 24Q Q4 data file passes TRACES FVU validation tool.
- Form 16 Part B for a sample employee matches TDS computation for the full financial year.
- Mid-year onboarding: employee with April–September YTD data entered shows correct annualised TDS projection for October onwards.

---

## Next Step

`/ce-plan` with this document as input (after Phase 2 is deployed).
