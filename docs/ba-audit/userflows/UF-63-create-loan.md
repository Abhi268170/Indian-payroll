# UF-63: Create Loan

**Module:** Loans > Create Loan
**Tested:** 2026-05-16
**Mock Data Used:** Existing loans: Arjun Mehta (LOAN-00001, Personal Loan, ₹50,000) and Vikram Nair (LOAN-00002, Emergency Loan, ₹1,00,000)
**App State Before:** Both loans status = Open, no EMI paid yet

## Steps Executed
1. Navigate to `#/loans` — observe loans list
2. Click LOAN-00001 (Arjun Mehta) — open loan detail panel
3. Open dropdown menu on LOAN-00001 — observe: Edit Loan, Pause Instalment Deduction, Delete Loan
4. Navigate to `#/loans/new` — open Create Loan form
5. Observe all form fields
6. Click Cancel — no new loan created

---

## Loans List Page (`#/loans`)

### Layout
- Left panel: loan list with summary cards
- Right panel: loan detail (entity details) for selected loan
- Header: "All Loans" button, "View Loan Repayments" link, "Add" button, overflow menu, filter icon, Instant Helper

### Table Columns
| Column | Type | Sortable |
|--------|------|---------|
| Employee Name | Text | Yes |
| Loan Number | Text (link) | Yes |
| Loan Name | Text | No |
| Status | Badge (Open/Closed/Paused) | No |
| Loan Amount | Currency | Yes |
| Amount Repaid | Currency | No |
| Remaining Amount | Currency | No |

### Existing Loans
| Loan # | Employee | Loan Name | Status | Amount | Repaid | Remaining |
|--------|----------|-----------|--------|--------|--------|-----------|
| LOAN-00001 | Arjun Mehta (EMP001) | Personal Loan | Open | ₹50,000.00 | ₹0.00 | ₹50,000.00 |
| LOAN-00002 | Vikram Nair (EMP003) | Emergency Loan | Open | ₹1,00,000.00 | ₹0.00 | ₹1,00,000.00 |

---

## Loan Detail Panel — LOAN-00001 (Arjun Mehta)

### Header
| Field | Value |
|-------|-------|
| Loan ID | LOAN-00001 |
| Status | Open |

### Primary Card
| Field | Value |
|-------|-------|
| Loan Name | Personal Loan |
| Total Amount | ₹50,000.00 |
| Availed by | Arjun Mehta (link to employee profile) |
| Instalment Amount | ₹5,000.00 |
| Status Progress | 0% (visual progress bar) |
| Next Instalment Date | 01/07/2026 |

### Other Details Section
| Field | Value |
|-------|-------|
| Disbursement Date | 01/06/2026 |
| Perquisite Rate | 0% |
| Loan Closing Date | 30/04/2027 |
| Reason | Personal loan for home renovation |

### EMI Math Verification
- Loan Amount: ₹50,000
- Instalment Amount: ₹5,000/month
- Instalments Remaining: 10
- Loan Duration: 10 months
- Total repayment: 10 × ₹5,000 = ₹50,000 ✓ (0% interest — no interest component)
- Disbursement: 01/06/2026 → First deduction: 01/07/2026 (one month grace period)
- Closing date: 30/04/2027 (10 months from July 2026 = April 2027) ✓

### Repayment Schedule
Empty state: "The employee is yet to pay the first instalment through Zoho Payroll."

Table headers: Instalment Date | EMI | Total Amount Repaid | Remaining Amount | Action

### Loan Actions (Dropdown Menu)
| Action | Purpose |
|--------|---------|
| Edit Loan | Modify loan details (before first deduction?) |
| Pause Instalment Deduction | Stop EMI from being deducted in next pay run |
| Delete Loan | Remove loan record |

### Direct Actions on Detail Panel
| Button | Purpose |
|--------|---------|
| Record Repayment | Manual entry of repayment outside pay run |

---

## Create Loan Form (`#/loans/new`)

### Form Fields
| Field | Label | Type | Required | Notes |
|-------|-------|------|----------|-------|
| Loan Name | Loan Name* | Dropdown (combobox) | Yes | Selects from pre-configured loan types; perquisite rate auto-fills |
| Perquisite Rate | (auto-filled, read-only) | Display | — | Shows 0% by default; linked to selected loan type |
| Employee Name | Employee Name* | Searchable combobox | Yes | Search and select from employee list |
| Loan Amount | Loan Amount* | Currency spinbutton | Yes | Prefixed with ₹ symbol; integer/decimal |
| Disbursement Date | Disbursement Date* | Date (dd/MM/yyyy) | Yes | Date loan is disbursed to employee |
| Reason | Reason* | Text (free text) | Yes | Reason for loan |
| Exempt from perquisite | Exempt this loan from perquisite calculation | Checkbox | No | Default unchecked |

### Perquisite Exemption Note
Verbatim text on form:
> "According to Rule 15(5) of the Income Tax Rules, 2026, employees availing medical loans (for treatment of diseases specified in Rule 18) or any other loans below ₹2,00,000 in aggregate can be exempted from perquisite calculation."

**Key statutory reference:** Rule 15(5) of IT Rules. Loans < ₹2,00,000 aggregate OR medical loans (Rule 18 diseases) are exempt from perquisite valuation.

### What Is Missing from Create Loan Form
The following fields are NOT on the creation form but are visible in loan details:
- Number of instalments / instalment amount (auto-calculated based on loan amount and type?)
- Interest rate / perquisite rate (auto-filled from loan type — but not editable here)
- First instalment date (auto-calculated from disbursement date?)

**Critical gap:** No instalment count or EMI amount visible at creation — the user doesn't see the repayment schedule before confirming.

### Form Actions
| Button | Action |
|--------|--------|
| Save | Creates loan record, generates repayment schedule |
| Cancel | Discards form, returns to loans list |

---

## Business Rules

1. Loan Name is chosen from a configured list (not free text) — loan types must be pre-configured in Settings > Loans
2. Perquisite Rate is determined by loan type, not entered directly
3. Loans below ₹2,00,000 aggregate can be exempted from perquisite via checkbox
4. Perquisite calculation follows Rule 15(5) of IT Rules — the system reference cites "Income Tax Rules, 2026" (likely errata — should be IT Rules, 1962, Rule 15(5))
5. First instalment begins in the month after disbursement (Arjun: disbursed June, first deduction July)
6. EMI is a flat amount (loan ÷ instalments) — 0% interest yields equal instalments
7. Loan is deducted automatically from the next applicable pay run after the first instalment date
8. Vikram Nair has a loan (₹1,00,000) even though he is SKIPPED in pay runs — the loan EMI will not deduct until he is included in a pay run

---

## Perquisite Calculation (Rule 15(5) reference)

If perquisite rate > 0%:
- Perquisite value = Outstanding loan balance × SBI lending rate (as notified by CBDT) / 12 per month
- If interest charged by employer ≥ SBI rate → no perquisite
- Perquisite is taxable under Section 17(2)(viii) as part of salary

For Arjun's loan: Perquisite Rate = 0% → system marks it as exempt (or relies on the checkbox) → no perquisite added to taxable income.

---

## Cross-Module Effects
- Loan EMI appears as a deduction line item in employee's monthly payslip
- In pay run Taxes & Deductions tab, loan EMI would appear under "Benefits" section
- Loan EMI is NOT taxable — it is a principal repayment, not income
- If perquisite applies (rate > 0%), perquisite value is added to gross income for TDS calculation
- When an employee exits (FnF), outstanding loan balance is typically deducted from final settlement

---

## Gaps / Observations
- 🔴 No instalment count or EMI preview before saving — user must calculate manually
- 🔴 Vikram Nair (onboarding incomplete, skipped from pay runs) has ₹1,00,000 loan — EMI cannot be deducted. No warning on the loan that the employee is payroll-ineligible.
- "Income Tax Rules, 2026" cited in the exemption note — likely a typo for "IT Rules, 1962" — should be verified
- No loan approval workflow visible — loans appear to be admin-created without employee consent workflow
- "View Loan Repayments" top-level link — leads to a consolidated repayment view (not yet explored)
- No bulk loan creation or import
- No maximum loan amount limit configured (UI doesn't show any cap)

## Open Questions
- [ ] Where are loan types configured? (Settings > Loans path not fully explored)
- [ ] Does the instalment count field appear after loan type is selected?
- [ ] What happens to outstanding loan when employee is terminated?
- [ ] Can an employee have multiple concurrent loans?
- [ ] Is there a loan approval workflow (employee request → admin approval)?
