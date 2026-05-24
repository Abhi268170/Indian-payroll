# Loans Module — BA Audit Index
**Date:** 2026-05-15
**Auditor:** BA Agent
**App URL:** https://payroll.zoho.in
**Org:** lerno (trial)
**Session:** Continuation from prior context

---

## Audit Items Covered

| Item | File | Status |
|------|------|--------|
| 87 | [Loans List — Empty State](87-loans-list-empty.md) | Complete |
| 88 | [Create Loan Form](88-create-loan.md) | Complete |
| 89 | [Loan Approval Flow](89-loan-approval.md) | Complete |
| 90 | [Loan Disbursement](90-loan-disbursement.md) | Complete |
| 91 | [Loans List — Populated State](91-loans-list-populated.md) | Complete |
| 92 | [Repayment Schedule View](92-repayment-schedule.md) | Complete |
| 93 | [Loan Deduction in Pay Run](93-loan-deduction-in-payrun.md) | Complete |
| 94 | [Loan Foreclosure / Lifecycle Actions](94-loan-foreclosure.md) | Complete |

---

## Test Data Created

### Loan 1
- **Loan ID:** LOAN-00001
- **Zoho Record ID:** 3848927000000034311
- **Employee:** Arjun Mehta (EMP001)
- **Loan Type:** Personal Loan (0% perquisite)
- **Amount:** ₹50,000
- **Disbursement Date:** 01/06/2026 (adjusted to future due to pay period constraint)
- **EMI Start Date:** 01/07/2026
- **Instalment Amount:** ₹5,000/month
- **Number of Instalments:** 10
- **Loan Closing Date:** 30/04/2027
- **Exempt from Perquisite:** Yes
- **Status:** Open

### Loan 2
- **Loan ID:** LOAN-00002
- **Zoho Record ID:** 3848927000000034328
- **Employee:** Vikram Nair (EMP003)
- **Loan Type:** Emergency Loan (6% perquisite)
- **Amount:** ₹1,00,000
- **Disbursement Date:** 01/06/2026
- **EMI Start Date:** 01/07/2026
- **Instalment Amount:** ₹8,607/month
- **Number of Instalments:** 12
- **Loan Closing Date:** 30/06/2027
- **Exempt from Perquisite:** No
- **Status:** Open

---

## Key Findings Summary

1. **Loan Types are admin-defined** — no system presets. Admin creates types via "Manage Loans" dialog with Loan Name + Perquisite Rate.
2. **Repayment section is conditional** — "EMI Deduction Start Date" and "Instalment Amount" fields only render after Disbursement Date is selected (triggered by bootstrap-datepicker's `changeDate` event).
3. **Pay period constraint on disbursement date** — cannot select a date in a completed pay period. Must be in a future pay period.
4. **Record Repayment date constraint** — repayment date must be > loan start date AND not in the future. Cannot record pre-payment for future-dated loans.
5. **Auto-calculation** — system auto-calculates "This loan will be fully paid off in N instalments" from Amount / EMI Amount.
6. **Foreclosure not found** — no explicit "Foreclose" action exists. Lifecycle management via: Edit Loan, Pause Instalment Deduction, Delete Loan, Record Repayment.
7. **Loan Repayments view** at `#/loans/repayments` is a separate page with Month/Method/Employee filters.
8. **Perquisite rate = 0%** triggers "Exempt this loan from perquisite calculation" checkbox (pre-checked, per Rule 15(5) IT Rules 2026).

---

## Screenshots Index

| File | Description |
|------|-------------|
| `87-loans-empty-state.png` | Loans list empty state |
| `87-manage-loans-dialog.png` | Manage Loans modal |
| `88-create-loan-form.png` | Create Loan form initial state |
| `88-create-loan-filled.png` | Create Loan form filled |
| `91-loan1-detail-created.png` | Loan 1 detail after creation |
| `91-loan2-detail-created.png` | Loan 2 detail after creation |
| `91-loans-list-populated.png` | Loans list with both loans |
| `91-loans-filter-dropdown.png` | Status filter dropdown (All/Open/Paused/Closed) |
| `92-loan1-repayment-schedule.png` | Loan 1 repayment schedule (pre-EMI) |
| `92-loan-repayments-page.png` | Loan Repayments tracking page |
| `90-record-repayment-modal.png` | Record Repayment modal |
| `94-loan-more-actions-dropdown.png` | More actions dropdown |
| `94-pause-instalment-modal.png` | Pause Instalment modal |
