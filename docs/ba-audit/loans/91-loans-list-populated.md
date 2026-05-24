# Item 91: Loans List — Populated State

**URL / Navigation Path:** `https://payroll.zoho.in/#/loans?filter_by=Status.All&page=1`
**Module:** Loans

---

## Purpose

The loans list with active loan records. Shows both loans created in this session.

---

## Screenshots

- `screenshots/91-loans-list-populated.png` — Full loans list with 2 loans
- `screenshots/91-loans-filter-dropdown.png` — Status filter dropdown
- `screenshots/91-loan1-detail-created.png` — Loan 1 (LOAN-00001) detail page
- `screenshots/91-loan2-detail-created.png` — Loan 2 (LOAN-00002) detail page

---

## Current State (Post-Creation)

Two loans visible in list:

| Employee | Loan Number | Loan Name | Status | Loan Amount | Amount Repaid | Remaining |
|----------|-------------|-----------|--------|-------------|---------------|-----------|
| Vikram Nair (EMP003) | LOAN-00002 | Emergency Loan | Open | ₹1,00,000.00 | ₹0.00 | ₹1,00,000.00 |
| Arjun Mehta (EMP001) | LOAN-00001 | Personal Loan | Open | ₹50,000.00 | ₹0.00 | ₹50,000.00 |

**Note:** List appears to be sorted by creation date descending (most recent first — LOAN-00002 appears above LOAN-00001).

---

## Table Columns (All Populated)

| Column | Data Type | Format | Sortable? |
|--------|-----------|--------|-----------|
| EMPLOYEE NAME | Text | "First Last (EMPXXX)" | URL has sort_column param — sortable |
| LOAN NUMBER | Text | "LOAN-XXXXX" (5 digits, sequential) | Likely sortable |
| LOAN NAME | Text | Admin-defined name | Likely sortable |
| STATUS | Badge/text | Open / Paused / Closed | Filter-able (see filter) |
| LOAN AMOUNT | Currency | ₹X,XX,XXX.XX (Indian format) | Likely sortable |
| AMOUNT REPAID | Currency | ₹X,XX,XXX.XX | Likely sortable |
| REMAINING AMOUNT | Currency | ₹X,XX,XXX.XX | Likely sortable |

---

## Status Filter (All Loans Dropdown)

Located in the top-left of the content area as "All Loans" dropdown.

Options:
- **All Loans** — default, shows all statuses
- **Open Loans** — active, EMI deductions in progress
- **Paused Loans** — deductions suspended
- **Closed Loans** — fully repaid or foreclosed

URL param: `filter_by=Status.All` | `filter_by=Status.Open` | `filter_by=Status.Paused` | `filter_by=Status.Closed`

---

## Toolbar Actions

| Action | Behaviour |
|--------|-----------|
| "View Loan Repayments" | Navigates to `#/loans/repayments` — cross-loan repayment history page |
| "Add" button | Navigates to `#/loans/new` — create new loan |

---

## Row Interaction

Row click navigates to loan detail: `#/loans/{record_id}` (the internal record ID, not LOAN-XXXXX).

The left panel of the detail view shows a **loan list sidebar** with all loans in a mini-list:
- Each shows: ₹Amount, Employee Name, Loan Type + Loan Number, Status badge
- Active loan highlighted

---

## Loan Detail Panel Fields (on detail view)

### Summary Header
- Large amount display: ₹X,XX,XXX.00
- Employee avatar (initial), employee name
- Loan Type + Loan Number + Status badge
- "Record Repayment" button (primary action)
- "More" button (kebab) → Edit Loan | Pause Instalment Deduction | Delete Loan

### Key Metrics (main panel)
| Label | Value |
|-------|-------|
| Loan Type | Personal Loan / Emergency Loan |
| Amount (large display) | ₹50,000.00 |
| Availed by | Employee name with avatar |
| Instalment Amount | ₹5,000.00 |
| Status | Progress bar (0%–100%) + "Next Instalment Date: DD/MM/YYYY" |

### Other Details (expandable section)
| Field | Value |
|-------|-------|
| Disbursement Date | 01/06/2026 |
| Perquisite Rate | 0% / 6% |
| Loan Closing Date | 30/04/2027 (auto-calculated) |
| Reason | Free text entered at creation |

### Loan Repayment Summary
| Label | Value |
|-------|-------|
| Amount Repaid | ₹0.00 |
| Remaining Amount | ₹50,000.00 |
| Instalment(s) Remaining | 10 |

### Repayment Schedule Table
Columns: INSTALMENT DATE | EMI | TOTAL AMOUNT REPAID | REMAINING AMOUNT | ACTION

Empty state: "The employee is yet to pay the first instalment through Zoho Payroll." (shown before first EMI pay run)

---

## Loan Number Format

LOAN-XXXXX — sequential, zero-padded to 5 digits:
- First loan: LOAN-00001
- Second loan: LOAN-00002

---

## Data Relationships

- Loan → Employee (many-to-one): each loan belongs to one employee
- Loan → Loan Type (many-to-one): each loan references one admin-defined loan type
- Loan → Pay Run (one-to-many): each pay run may process zero or more loan EMI deductions
- Loan → Repayment Records (one-to-many): each repayment (via pay run or manual) creates a record

---

## Open Questions

- [ ] Can one employee have multiple active loans simultaneously?
- [ ] Is there a maximum number of loans per employee?
- [ ] Does the list support pagination? (URL has `page=1`)
- [ ] Is the LOAN-XXXXX number org-level or global sequential?
