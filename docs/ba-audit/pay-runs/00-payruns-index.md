# Pay Runs Module — BA Audit Index

**Application:** Zoho Payroll India (`payroll.zoho.in`)
**Organisation:** lerno (trial account)
**Module:** Pay Runs
**Audit Period:** April–May 2026 (live runs created and executed)
**Pay Run ID (May 2026):** `3848927000000034159`
**Auditor:** BA Agent (Playwright MCP)

---

## Audit Files

| # | File | Topic | Status |
|---|------|-------|--------|
| 52 | [52-payrun-module.md](52-payrun-module.md) | Module overview, full lifecycle, April 2026 run baseline | Complete |
| 53 | [53-create-period-selection.md](53-create-period-selection.md) | Pay run initiation, period card, auto-creation behaviour | Complete |
| 54 | [54-create-variable-inputs.md](54-create-variable-inputs.md) | LOP entry, Add Earning, TDS override, Import/Export | Complete |
| 55 | [55-create-review-screen.md](55-create-review-screen.md) | Draft summary page, all tabs, pending tasks, per-row kebab | Complete |
| 56 | [56-payslip-preview-in-run.md](56-payslip-preview-in-run.md) | Payslip split panel (read-only), TDS Sheet PDF iframe | Complete |
| 57 | [57-proration-midmonth.md](57-proration-midmonth.md) | LOP proration formula, mid-month joiner gap, edge cases | Complete |
| 58 | [58-ff-midmonth-exit.md](58-ff-midmonth-exit.md) | F&F settlement — gap analysis (EMP005 skipped) | Complete |
| 59 | [59-approval-flow.md](59-approval-flow.md) | Draft → Approved transition, pending task gate, Skip dialog | Complete |
| 60 | [60-post-approval-state.md](60-post-approval-state.md) | Approved state, Record Payment, Paid state, full state machine | Complete |
| 61 | [61-payment-advice-export.md](61-payment-advice-export.md) | Bank Advice download, Export Data, Downloads panel | Complete |
| 62 | [62-payslip-full-view.md](62-payslip-full-view.md) | Payslip field inventory, TDS Sheet content, new regime slabs | Complete |
| 63 | [63-payslip-email.md](63-payslip-email.md) | Bulk/individual payslip email, Send Payslip flow | Complete |
| 64 | [64-june-baseline-run.md](64-june-baseline-run.md) | Payroll History table, next-run card timing | Complete |
| 65 | [65-reprocess-revise.md](65-reprocess-revise.md) | Delete Recorded Payment, Revise Salary, per-state kebab matrix | Complete |
| 66 | [66-arrears-run.md](66-arrears-run.md) | One Time Payout, Off Cycle Pay Run, Resettlement Payroll | Complete |
| 67 | [67-statutory-summary.md](67-statutory-summary.md) | Overall Insights, Statutory Summary (empty), component drill-down | Complete |

**Total: 16 files + this index = 17 files**

---

## Pay Run State Machine (Summary)

```
READY
  ↓  [click period card]
DRAFT
  ├─ Enter variable inputs: LOP, Add Earning, TDS Override
  ├─ Manage pending tasks (Skip employees, add bank details, etc.)
  ↓  [Approve Payroll — all tasks complete]
APPROVED
  ├─ [Reject Approval + optional reason] → DRAFT
  ↓  [Record Payment — date, mode, reference]
PAID
  └─ [Delete Recorded Payment] → APPROVED
       └─ [Reject Approval] → DRAFT
```

---

## Key Business Rules Discovered

| Rule | Source | Notes |
|------|--------|-------|
| Sequential monthly runs only | Period card auto-advances | Cannot manually select period; admin cannot pre-initiate future month |
| Hard block on approval if pending tasks | Approval gate | Toast: "Please complete your pending tasks" |
| Skip requires mandatory reason | Skip dialog | Reason stored and displayed permanently in employee row |
| LOP proration: (Base Days − LOP) / Base Days × Amount | Confirmed via EMP001 (2 LOP days, May 2026) | Calendar days, not business days |
| Mid-month joiners NOT auto-prorated | Confirmed via EMP002 (joined 16 May) | Admin must manually enter LOP days for days before join date |
| TDS override requires mandatory reason | TDS inline form | Cannot save override without reason |
| Payslip password protection: default ON | Download dialog | Checkbox pre-checked; password protects PDF |
| Skipped employees visible permanently | Post-approval/Paid table | With reason; no checkbox, no kebab, no pay data |
| Delete Recorded Payment: Paid → Approved | Confirmed via dialog | Does not revert to Draft; inputs remain locked |
| New regime TDS only (v1) | TDS Sheet PDF | Old regime not available in this org configuration |
| Standard Deduction FY2026 (new regime): ₹75,000 | TDS Sheet | Confirmed from TDS computation worksheet |

---

## Critical Gaps Identified (for Our Build)

| Gap | Priority | Notes |
|-----|----------|-------|
| No auto-proration for mid-month joiners | High | Zoho requires manual LOP entry. Our build: auto-compute LOP from joining date |
| No dedicated F&F settlement flow | High | Zoho uses regular/off-cycle run + manual inputs. Our build: explicit F&F run type with gratuity auto-compute |
| No single "Reprocess" command | Medium | Zoho requires 5 manual steps to go from Paid back to Draft. Our build: one-click reprocess |
| Downloads panel doesn't track Bank Advice | Medium | Bank Advice is a direct download. Our build: store all generated files in MinIO with download history |
| No date range filter in Payroll History | Low | Only Payroll Type filter. Our build: add from/to date filter |
| Statutory Summary shows "no data" with no context | Low | No explanation for empty state. Our build: explain why it's empty + link to Settings |
| TDS Sheet is PDF-only | Medium | Cannot extract structured data. Our build: expose TDS computation as JSON API + PDF download |
| No per-employee export | Low | All exports are bulk. Our build: support single-employee data download |

---

## Screenshots Directory

All screenshots in: `docs/ba-audit/pay-runs/screenshots/`

Key screenshots by item:
- Item 53: `53-pay-runs-list.png`, `53-preview-draft-initial.png`, `53-pending-tasks-expanded.png`, `53-add-employees-missing.png`, `53-skip-employee-dialog.png`, `53-all-employees-skipped.png`
- Item 54: `54-emp001-split-panel.png`, `54-lop-entry-panel.png`, `54-lop-2days-before-save.png`, `54-emp001-after-lop-save.png`, `54-add-earning-dropdown.png`, `54-tds-edit-inline-form.png`, `54-import-export-menu.png`, `54-import-one-time-earnings.png`
- Item 55: `55-taxes-deductions-tab.png`, `55-overall-insights-tab.png`, `55-employee-row-kebab-draft.png`
- Item 56: `56-payslip-view-post-approval.png`, `56-tds-sheet-modal.png`
- Item 57: `57-emp002-proration-panel.png`
- Item 59: `59-approval-hard-block-toast.png`, `59-approve-payroll-dialog.png`, `59-post-approval-summary.png`
- Item 60: `60-approved-kebab-menu.png`, `60-reject-approval-dialog.png`, `60-record-payment-dialog.png`, `60-paid-state-summary.png`, `60-paid-page-kebab.png`, `60-paid-row-kebab.png`
- Item 61: `61-downloads-panel-empty.png`, `61-export-data-options.png`
- Item 62: `62-download-payslip-dialog.png`
- Item 63: `63-send-payslip-dialog.png`
- Item 64: `64-payroll-history-two-runs.png`
- Item 65: `65-paid-kebab-menu.png`, `65-delete-recorded-payment-dialog.png`, `65-paid-row-kebab-options.png`
- Item 66: `66-new-dropdown-options.png`, `66-off-cycle-payrun-dialog.png`, `66-one-time-payout-dialog.png`
- Item 67: `67-overall-insights-statutory.png`

---

## Next Audit Sessions (Recommended)

| Module | Priority | Notes |
|--------|----------|-------|
| TDS / Income Tax — Form 16, Quarterly Returns | High | Feeds from payroll run data |
| Provident Fund — ECR file generation | High | Requires PF-configured org |
| ESI — ESI returns, Challan | High | Requires ESI-configured org |
| Professional Tax — state-wise PT | Medium | Kerala PT in this org — configure and test |
| Reports module — all report types | Medium | Payroll Summary, Bank Transfer, Statutory reports |
| Settings — Statutory Config (PF/ESI limits, tax slabs) | High | Must configure before statutory tests |
| Employee Master — all sub-pages | High | PAN, bank details, salary structure assignment |
| Off Cycle Pay Run — full flow with onboarded employee | Medium | F&F use case requires fully onboarded employee |
