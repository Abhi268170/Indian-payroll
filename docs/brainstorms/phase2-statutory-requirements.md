---
date: 2026-05-13
topic: indian-payroll-phase2-statutory
master: docs/brainstorms/indian-payroll-saas-requirements.md
---

# Phase 2 — Statutory Compliance

## Goal

Add all statutory deductions and compliance outputs on top of the Phase 1 foundation. After Phase 2, payroll runs produce accurate TDS, PF, ESI, PT, LWF deductions; variable inputs (LOP, ad-hoc components) feed into gross; and compliance files (PF ECR, ESI challan, TDS working sheet, pay register) are exportable.

**Prerequisite:** Phase 1 complete and deployed.

---

## Actors

- A2. Org Admin — toggle statutory modules, configure slabs
- A4. Payroll/Finance Manager — upload variable inputs, download compliance files
- A5. Employee — payslip now shows statutory deductions
- A6. Background Worker — statutory computation in batch

---

## Key Flows (Phase 2 scope)

- **F1 (complete):** Full monthly payroll run with variable inputs file, LOP, all statutory deductions, gross → net. Draft → Finalised. Payslip includes statutory detail.
- **F6 (partial):** Pay register export, TDS working sheet export, PF ECR download, ESI challan download, PT register. (Large async exports in Phase 4.)

---

## Requirements

**Statutory Module Toggles**
- R0a. Org Admin enables each statutory module per tenant: PF, ESI, PT (Kerala), LWF (Kerala), VPF. Toggle effective-date-logged. Disabled modules produce ₹0 in payroll and are excluded from challans.

**Variable Pay & Per-Run Inputs**
- R40. Variable inputs file (CSV/Excel): Employee ID, LOP days, ad-hoc earning/deduction columns. Column headers must match configured component names exactly.
- R41. File validation: employee IDs active, LOP days non-negative and ≤ working days, numeric values. Per-row error report. Upload rejected until resolved.
- R42. Variable inputs file stored as immutable audit artifact per run. Re-upload retains prior version in audit log.
- R43. No file uploaded → LOP = 0, no ad-hoc components. Warning banner shown before run trigger.

**Core Payroll Engine (complete)**
- R11 (full). Gross = fixed components + variable earning inputs − LOP. Statutory deductions: TDS, PF employee, ESI employee (if applicable), PT, LWF. Employer contributions: PF employer (EPF + EPS + EDLI + admin), ESI employer. Net pay = gross − all deductions.
- R12. LOP = (monthly gross / working days) × LOP days. Working days configurable.
- R13. Arrears as distinct line item (from Phase 3 salary revision — scaffold the line item slot in Phase 2 payslip layout even if arrears computation lands in Phase 3).

**TDS — New Regime Only**
- R18. New regime only (Section 115BAC).
- R19. Monthly TDS: annualise YTD + projected remaining months → compute annual tax → deduct already-paid TDS → divide remaining tax across remaining months.
- R20. Standard deduction ₹75,000 before tax computation.
- R21. Tax slabs stored as configurable data. FY 2025-26: ₹0–4L 0%, ₹4–8L 5%, ₹8–12L 10%, ₹12–16L 15%, ₹16–20L 20%, ₹20–24L 25%, >₹24L 30%.
- R22. Surcharge slabs: ₹50L–₹1Cr 10%, ₹1Cr–₹2Cr 15%, >₹2Cr 25% (new regime cap). Marginal relief at each boundary. 4% health + education cess on (tax + surcharge). All rates configurable data.
- R24. TDS shortfall redistributed across remaining months in FY.

**Provident Fund**
- R25. Employee PF: 12% of (Basic + DA). VPF: optional fixed amount or % above 12%, per employee election, included in ECR.
- R26. Employer split: EPF 3.67%, EPS 8.33% (capped ₹1,250/month), EDLI 0.50% (capped ₹75/month), EPF admin 0.50% (min ₹500/month at tenant level). All rates configurable.
- R27. PF opt-out: Basic > ₹15,000 → opt-out allowed at joining. Mid-employment threshold-crossing = continue unless explicit opt-out.
- R28. PF ECR file per finalised monthly run in EPFO UAN portal format.

**ESI**
- R29. Coverage: gross ≤ ₹21,000 (₹25,000 PWD). New joiners at or below threshold covered from day 1. Above threshold at joining = not covered for that period. Review at period boundaries (April, October).
- R30. Employee 0.75%, employer 3.25% of gross. Gross includes variable inputs (ad-hoc earnings).
- R31. Mid-period threshold crossing: covered through end of period. Stops next period if still above threshold.
- R32. ESI challan file per contribution period (Apr–Sep, Oct–Mar).

**Professional Tax — Kerala Only**
- R33. State slab config as data. PT module designed for multi-state addition later.
- R34. Kerala PT: two periods (Apr–Sep, Oct–Mar). Monthly deduction = Kerala slab for cumulative period gross ÷ remaining months in period. Slab values configurable.

**Labour Welfare Fund — Kerala Only**
- R35. Kerala LWF: annual deduction in December. Employee + employer contribution amounts configurable. Employment-on-rolls check on deduction date.

**Statutory Returns & Exports**
- R36 (partial). Form 24Q data generation — Q1/Q2/Q3 only in Phase 2. Q4 + annual reconciliation in Phase 3.
- R38. PF ECR file downloadable from any finalised monthly run.
- R39. ESI challan file downloadable per contribution period.
- R46. Pay register Excel/PDF export from any finalised run (sync, ≤1,000 employees; async Phase 4).
- R47. PF monthly statement, ESI monthly statement, PT register, challan summaries.
- R54 (partial). Async export for >1,000 employees — Phase 4; Phase 2 sync only.
- R69. TDS working sheet Excel: Employee Name, PAN, Monthly Gross, YTD Gross, YTD Off-Cycle Payouts, Projected Annual Income, Standard Deduction, Taxable Income, Tax Computed, TDS Deducted YTD, Balance TDS.
- R70. Individual employee payroll detail export (PDF + Excel) from pay register view.

---

## Out of Scope (Phase 2)

- Section 89(1) arrears relief (Phase 3 — needs FnF/revision context)
- Annual TDS reconciliation + Form 16 (Phase 3)
- Form 24Q Q4 (Phase 3)
- Salary revision + arrears computation (Phase 3)
- FnF (Phase 3)
- Off-cycle runs (Phase 3)
- Loan EMI deductions (Phase 4)
- Async bulk exports >1,000 employees (Phase 4)
- PT/LWF for states other than Kerala (future)

---

## Success Criteria

- Payroll run for a sample employee with known CTC produces TDS, PF, ESI, PT, LWF deductions matching hand-calculated values using FY 2025-26 statute rules.
- PF ECR file passes EPFO UAN portal format validation.
- ESI challan file passes ESIC portal format validation.
- Disabling PF module for a tenant excludes all PF lines from payroll, payslip, and ECR for that tenant.
- TDS working sheet columns match TDS projection formula for 3 sample employees across different income brackets (including surcharge boundary).

---

## Next Step

`/ce-plan` with this document as input (after Phase 1 is deployed).
