# Item 92: Repayment Schedule View

**URL / Navigation Path:**
- Loan detail: `https://payroll.zoho.in/#/loans/{record_id}`
- Repayment tracking: `https://payroll.zoho.in/#/loans/repayments?employee_id=&filter_by=&month=2026-05&page=1&per_page=50&repayment_type=&selected_label=`

**Module:** Loans

---

## Purpose

Two distinct repayment views:
1. **Inline repayment schedule** on the Loan Detail page — instalment-by-instalment breakdown for a single loan
2. **Loan Repayments tracking page** (`#/loans/repayments`) — cross-loan repayment history with month/method/employee filters

---

## Screenshots

- `screenshots/92-loan1-repayment-schedule.png` — Loan 1 detail with repayment schedule
- `screenshots/92-loan-repayments-page.png` — Loan Repayments tracking page

---

## View 1: Inline Repayment Schedule (on Loan Detail)

### Location
Within the Loan Detail page, below the "Loan Repayment Summary" section.

### Summary Cards

| Label | Value (Loan 1) |
|-------|----------------|
| Amount Repaid | ₹0.00 |
| Remaining Amount | ₹50,000.00 |
| Instalment(s) Remaining | 10 |

### Repayment Schedule Table

| Column | Description |
|--------|-------------|
| INSTALMENT DATE | Scheduled date for each EMI deduction |
| EMI | Amount for this instalment (₹) |
| TOTAL AMOUNT REPAID | Cumulative repaid after this instalment |
| REMAINING AMOUNT | Principal remaining after this instalment |
| ACTION | Action links (e.g., modify, skip — not directly observed as no data yet) |

**Empty State (before first pay run processes an EMI):**
"The employee is yet to pay the first instalment through Zoho Payroll."

The table is populated when:
1. A pay run processes the EMI deduction for a given month, OR
2. A manual repayment is recorded via "Record Repayment" modal

### Key Calculated Fields

**Loan Closing Date** — auto-calculated on creation:
- Formula: EMI Start Date + (N-1) months, where N = ceiling(Loan Amount / Instalment Amount)
- Loan 1: 10 instalments starting 01/07/2026 → closes 30/04/2027
- Loan 2: 12 instalments starting 01/07/2026 → closes 30/06/2027

**Status Progress Bar**
- Shows 0%–100% of loan repaid
- Updates after each instalment processed

**Next Instalment Date**
- Displayed below the progress bar
- Shows the upcoming EMI date

---

## View 2: Loan Repayments Tracking Page

**URL:** `#/loans/repayments`
**Accessible from:** "View Loan Repayments" link on Loans list toolbar

### Filters

| Filter | Type | Options / Format |
|--------|------|-----------------|
| Month | Date selector/dropdown | Format: YYYY-MM (e.g., 2026-05). Defaults to current month. |
| Repayment Method | Dropdown | "Loan Repayment Method" placeholder — options not expanded in session |
| Employees | Combobox | "Select an Employee" — multi-select or single |

**Filter URL params:** `employee_id=&filter_by=&month=2026-05&page=1&per_page=50&repayment_type=&selected_label=`

### Table (Empty in Current Session)

**Empty State:** "No loan repayments match the selected filter."

Table columns (inferred from other similar Zoho pages — not observed with data):
Expected: Employee Name, Loan Number, Loan Name, Instalment Date, EMI Amount, Payment Mode, Status

### Repayment Types (inferred from URL param `repayment_type=`)
Likely: Through Payroll / Manual (Record Repayment modal)

---

## Business Rules

1. Instalment schedule is computed at loan creation: N = Loan Amount / Instalment Amount
2. EMI deduction is automatic for each subsequent pay run from EMI Start Date
3. Manual repayment (via "Record Repayment") must have date > loan start date AND not in future
4. If loan amount is not exactly divisible by instalment amount, the final instalment adjusts to cover remainder (standard EMI behaviour — exact rounding not observed)
5. Loan closing date is fixed at creation based on N and EMI start date

---

## Cross-Module Impact

- Pay run processes EMI deductions → populates repayment schedule rows
- Repayment schedule drives TDS/perquisite calculations for non-exempt loans
- "Amount Repaid" and "Remaining Amount" drive the foreclosure/closure state

---

## Open Questions

- [ ] What happens to the repayment schedule if the instalment amount is changed (Edit Loan)?
- [ ] Can individual instalments be skipped or deferred?
- [ ] What is the ACTION column in the repayment table? (Edit individual instalment? Skip?)
- [ ] Does the Loan Repayments page show payroll-deducted EMIs vs manual repayments separately?
- [ ] If employee leaves mid-loan, what happens to the schedule?
- [ ] Is there a partial payment scenario (less than full EMI in a pay run)?
