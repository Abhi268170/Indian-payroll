# UF-80: Loan Reports

**Module:** Reports > Loan Reports
**Tested:** 2026-05-16
**Mock Data Used:** LOAN-00001 (Arjun Mehta, Personal Loan, ₹50,000) and LOAN-00002 (Vikram Nair, Emergency Loan, ₹1,00,000)
**App State Before:** Both loans Open; no EMIs paid yet

## Steps Executed
1. Identified 4 loan reports from Reports Centre overview
2. Cross-referenced with loan data captured in UF-63
3. Documented expected content

---

## Report 1: Loan Outstanding Summary

**Purpose:** Current outstanding loan balance per active loan. Point-in-time snapshot of all loans.

**Expected Columns:**
| Column | LOAN-00001 | LOAN-00002 |
|--------|-----------|-----------|
| Loan Number | LOAN-00001 | LOAN-00002 |
| Employee | Arjun Mehta | Vikram Nair |
| Loan Name | Personal Loan | Emergency Loan |
| Loan Amount | ₹50,000 | ₹1,00,000 |
| Disbursement Date | 01/06/2026 | (not confirmed) |
| Amount Repaid | ₹0.00 | ₹0.00 |
| Outstanding Balance | ₹50,000 | ₹1,00,000 |
| Instalment Amount | ₹5,000 | (not confirmed) |
| Instalments Remaining | 10 | (not confirmed) |
| Status | Open | Open |
| Next EMI Date | 01/07/2026 | (not confirmed) |

**Use Case:** Monthly reconciliation of employer loans outstanding. Finance team monitors total receivable from employees.

---

## Report 2: Loan Perquisite Summary

**Purpose:** Perquisite value generated from interest-free or subsidized loans per employee per month. Required for TDS computation.

**Perquisite Calculation Reference (Rule 15(5)):**
- Perquisite value = (Outstanding loan × SBI MCLR lending rate / 12) per month
- If employer charges interest ≥ SBI rate → perquisite = ₹0
- If employer charges 0% → perquisite = outstanding × SBI rate / 12

**Current Data:**
- Both loans have Perquisite Rate = 0% AND Exempt checkbox may be checked
- Arjun's loan: Outstanding ₹50,000 × SBI rate (e.g., 9.5%) / 12 = ₹395.83/month perquisite value (if not exempt)
- Vikram's loan: Outstanding ₹1,00,000 × 9.5% / 12 = ₹791.67/month (if not exempt)

**Expected Report Columns:**
| Column | Arjun (May 2026) |
|--------|-----------------|
| Employee | Arjun Mehta |
| Loan | LOAN-00001 |
| Month | May 2026 |
| Opening Balance | ₹50,000 |
| Closing Balance | ₹50,000 (no EMI deducted yet) |
| SBI Rate Used | e.g., 9.50% |
| Employer Rate | 0% |
| Perquisite Value | ₹0 (if exempt) or ₹395.83 |
| Exemption Applied | Yes (if < ₹2L aggregate) |
| Taxable Perquisite | ₹0 |

---

## Report 3: Loan Perquisite Projection

**Purpose:** Future projection of perquisite values based on outstanding loan balances and repayment schedule. Useful for annual TDS planning.

**Expected Content:**
- Month-by-month projection through loan closing date
- As principal reduces (EMI deducted), perquisite reduces proportionally
- Final month: outstanding approaches ₹0, perquisite approaches ₹0

**Sample Projection for LOAN-00001 (Arjun, ₹50,000, 10 EMIs of ₹5,000):**
| Month | Opening Balance | EMI Deducted | Closing Balance | Perquisite (if applicable) |
|-------|----------------|-------------|----------------|---------------------------|
| Jul 2026 | ₹50,000 | ₹5,000 | ₹45,000 | ₹395.83 (if not exempt) |
| Aug 2026 | ₹45,000 | ₹5,000 | ₹40,000 | ₹356.25 |
| ... | ... | ₹5,000 | ... | decreasing |
| Apr 2027 | ₹5,000 | ₹5,000 | ₹0 | ₹39.58 |

**Use:** TDS officer can estimate total perquisite for Form 16 Part B in advance.

---

## Report 4: Loan Summary Report

**Purpose:** Comprehensive view of all loans — current and historical — with status, amounts, and repayment progress.

**Expected Columns:**
| Column | LOAN-00001 | LOAN-00002 |
|--------|-----------|-----------|
| Loan Number | LOAN-00001 | LOAN-00002 |
| Employee | Arjun Mehta | Vikram Nair |
| Loan Name | Personal Loan | Emergency Loan |
| Loan Amount | ₹50,000 | ₹1,00,000 |
| Total Repaid | ₹0 | ₹0 |
| Outstanding | ₹50,000 | ₹1,00,000 |
| Instalment | ₹5,000 | (unknown) |
| Instalments Paid | 0 | 0 |
| Instalments Remaining | 10 | (unknown) |
| Disbursement Date | 01/06/2026 | (unknown) |
| Closing Date | 30/04/2027 | (unknown) |
| Status | Open | Open |
| Perquisite Rate | 0% | 0% |
| Exempt | Yes/No | Yes/No |

---

## Business Rules for Loan Reports
1. Loan perquisite report should be generated monthly — even if perquisite = ₹0 (for audit trail)
2. Perquisite value feeds into employee's taxable income for TDS computation
3. When EMI deducted in pay run, the loan outstanding reduces — report reflects updated balance
4. For terminated employees: FnF deducts outstanding balance; loan shows status "Closed" in report
5. Paused loans: Instalment Amount still shows but "Paused" period is noted; balance does not reduce during pause

## Gaps / Observations
- Loan reports not individually opened — content inferred from loan data in UF-63
- 🟡 Mark for future session: open actual loan reports and verify perquisite computation
- Vikram's loan (LOAN-00002): Vikram is skipped from pay runs — his loan EMI will never deduct until onboarding complete; report will show perpetual outstanding balance
- No SBI MCLR rate configuration visible — unclear if Zoho hardcodes it or allows admin to update it periodically

## Open Questions
- [ ] Where is the SBI MCLR rate configured? Is it a system-maintained value or admin-configurable?
- [ ] Does Loan Perquisite Summary generate monthly rows even for paused loans?
- [ ] If an employee is terminated with outstanding loan balance — does the FnF report show the deduction?
- [ ] Can multiple loans per employee aggregate for the ₹2,00,000 perquisite exemption threshold?
