# Item 87: Loans List — Empty State

**URL / Navigation Path:** `https://payroll.zoho.in/#/loans`
**Route Pattern:** `#/loans?filter_by=Status.All&manage_loans=false&page=1&selected_label=&sort_column=&sort_order=`
**Module:** Loans
**Access Roles:** Admin, HR Manager (inferred — no explicit role UI observed)

---

## Purpose

Entry point for the Loans module. Displays all employee loans as a master list. In empty state (no loans created), shows a blank table with column headers and a "Manage Loans" entry point.

---

## Screenshots

- `screenshots/87-loans-empty-state.png` — Full page empty state
- `screenshots/87-manage-loans-dialog.png` — Manage Loans modal (loan type config)
- `screenshots/87-manage-loans-with-personal.png` — After creating Personal Loan type

---

## Layout

**Two-panel layout:**
- Left sidebar: Zoho Payroll global navigation (Dashboard, Employees, Pay Runs, Approvals, Taxes & Forms, Loans, Giving, Documents, Reports, Settings)
- Right content area: Loans list with toolbar + table + optionally the Manage Loans inline dialog panel at the bottom

**Top toolbar (above table):**
- Title: "All Loans" (dropdown — acts as status filter)
- "View Loan Repayments" link → navigates to `#/loans/repayments`
- "Add" button → navigates to `#/loans/new`

**Table (empty state):**
- Column headers rendered even when empty
- No empty state illustration/icon observed — just empty rows

---

## Table Columns

| Column | Description |
|--------|-------------|
| EMPLOYEE NAME | Employee display name + employee code (e.g., "Arjun Mehta (EMP001)") |
| LOAN NUMBER | System-assigned sequential ID (e.g., LOAN-00001) |
| LOAN NAME | Loan type name (admin-defined) |
| STATUS | Open / Paused / Closed |
| LOAN AMOUNT | Original principal amount (₹ formatted, 2 decimal places) |
| AMOUNT REPAID | Total repaid to date (₹ formatted) |
| REMAINING AMOUNT | Outstanding principal (₹ formatted) |

---

## Manage Loans Sub-feature

Accessible via dropdown on the Loans list toolbar (chevron/dropdown beside table header area) or URL param `manage_loans=true`.

**Manage Loans modal/panel shows:**
- Table: LOAN NAME | PERQUISITE RATE | ACTIONS
- Each row: loan type name, perquisite rate (%), Edit (pencil) + Delete (trash) icons
- "+ Create New Loan Type" link — adds inline editable row

**Create New Loan Type inline form fields:**

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Loan Name | Text input | Yes | Non-empty | Free text, no format restriction observed |
| Perquisite Rate | Number input | Yes | Numeric | Percentage (0–100). 0% = loan interest-free per statutory exemption |

**Actions on Manage Loans row:**
- Save (tick/check button) — saves the new loan type
- Cancel — discards
- Edit existing row — same inline form
- Delete existing row — no confirmation dialog observed for loan types (only for loans themselves)

**Toast on Save:** "Loan Type has been created successfully."

**Business Rule:** Perquisite rate is set at loan type level, not individual loan level. All loans of a given type inherit the rate.

**Statutory context:** Rule 15(5) of the Income Tax Rules, 2026 — employees availing medical loans (diseases specified in Rule 18) or aggregate loans below ₹2,00,000 may be exempted from perquisite calculation.

---

## Filters (discovered from populated state)

"All Loans" dropdown filter:
- All Loans (default)
- Open Loans
- Paused Loans
- Closed Loans

URL param: `filter_by=Status.All` / `filter_by=Status.Open` etc.

---

## Navigation

- **Entry points:** Sidebar "Loans" nav item; direct URL
- **Out:** "Add" → Create Loan; "View Loan Repayments" → `#/loans/repayments`; row click → Loan Detail

---

## Open Questions

- [ ] What roles can access the Loans module (Viewer vs Admin)?
- [ ] Is there pagination on the loans list? (URL has `page=1` param)
- [ ] Is there a search/filter by employee name or loan number?
- [ ] Does "sort_column" URL param support column sorting on click?
- [ ] What happens if a loan type is deleted while active loans use it?
