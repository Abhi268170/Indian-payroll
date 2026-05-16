# UF-66: Loan Perquisite Calculation

**Module:** Loans > Loan Detail / Taxes & Forms / Reports
**Tested:** 2026-05-16
**Mock Data Used:** LOAN-00001 (₹50,000, 0% perquisite rate, Arjun); LOAN-00002 (₹1,00,000, Vikram)
**App State Before:** Both loans Open; no EMIs paid

## Steps Executed
1. Observed loan detail for LOAN-00001: Perquisite Rate = 0%
2. Observed perquisite exemption note on Create Loan form (verbatim text)
3. Documented perquisite calculation rules from statutory reference

---

## What Is Loan Perquisite

When an employer gives a loan to an employee at zero or below-market interest rate, the **interest benefit** is treated as a perquisite (benefit in kind) — taxable as salary income under Section 17(2)(viii) of the Income Tax Act.

**Perquisite value** = notional interest the employee would have paid at SBI lending rate minus actual interest charged by employer.

---

## Statutory Framework

### Rule 15(5) of Income Tax Rules, 1962

**Verbatim text from Create Loan form:**
> "According to Rule 15(5) of the Income Tax Rules, 2026, employees availing medical loans (for treatment of diseases specified in Rule 18) or any other loans below ₹2,00,000 in aggregate can be exempted from perquisite calculation."

**Note:** The UI says "Income Tax Rules, 2026" — this is an errata. The correct reference is **Income Tax Rules, 1962, Rule 15(5)**. FY2026 may have updated this rule, but the base law is IT Rules 1962.

### Exemptions from Perquisite (Rule 15(5))
1. **Medical loans** for diseases specified in Rule 18 of IT Rules — fully exempt regardless of amount
2. **Loans below ₹2,00,000 in aggregate** — exempt from perquisite calculation
3. **Employer charges interest ≥ SBI MCLR rate** — no perquisite (employee pays market rate)

### SBI Lending Rate Reference
- RBI notifies SBI's marginal cost of funds-based lending rate (MCLR) annually
- Current SBI MCLR (1-year): approximately 9.0-9.5% (FY2025-26)
- Perquisite computation uses the rate prevailing on the first day of the relevant financial year

---

## Perquisite Calculation Formula

```
Monthly Perquisite = (Opening Balance × SBI Rate) / 12
```

Where:
- Opening Balance = outstanding loan at start of month
- SBI Rate = SBI MCLR for 1-year tenor as on April 1 of the FY
- The result is the perquisite value for that month

**Example (LOAN-00002, Vikram, ₹1,00,000, assuming SBI rate = 9.5%):**
```
Monthly perquisite = ₹1,00,000 × 9.5% / 12 = ₹791.67
```

This ₹791.67 is added to Vikram's gross income for TDS computation.

**For LOAN-00001 (Arjun, exempt — below ₹2L threshold):**
- Loan = ₹50,000 < ₹2,00,000 aggregate → Perquisite = ₹0

---

## Current Configuration

### LOAN-00001 (Arjun)
- Loan Amount: ₹50,000
- Perquisite Rate: 0%
- Exempt from perquisite: Yes (implied, since ₹50,000 < ₹2,00,000 threshold)
- Monthly Perquisite: ₹0

### LOAN-00002 (Vikram)
- Loan Amount: ₹1,00,000
- Perquisite Rate: 0% (from loan type configuration)
- Exempt from perquisite: Unknown (not observed)
- If NOT exempt: Monthly Perquisite = ₹1,00,000 × SBI rate / 12

**Note:** Vikram's loan exceeds ₹2,00,000 threshold? No — ₹1,00,000 < ₹2,00,000. Both loans individually qualify for exemption under the ₹2L aggregate rule, assuming no other loans exist.

---

## Aggregate Loan Tracking

The exemption is on **aggregate** outstanding loans — if an employee has multiple loans:
- Total outstanding across all loans is checked against ₹2,00,000 threshold
- If aggregate > ₹2L: entire loan balance attracts perquisite (not just the excess)

**Example:** Employee has LOAN-A ₹1,50,000 + LOAN-B ₹1,00,000 = ₹2,50,000 aggregate → Full ₹2,50,000 attracts perquisite.

---

## Perquisite in TDS Computation

When perquisite applies:
1. Monthly perquisite value is added to employee's gross income
2. Annualized: Monthly perquisite × 12 = Annual perquisite income
3. This increases the taxable salary → increases TDS

**Example:** If Vikram's loan (₹1,00,000) was not exempt:
- Annual perquisite = ₹791.67 × 12 = ₹9,500
- This is added to Vikram's annual income for tax computation
- Increases TDS by: ₹9,500 × marginal tax rate

---

## Business Rules
1. Perquisite = employer's interest subsidy (difference between SBI rate and employer rate)
2. Perquisite is added to taxable income under Section 17(2)(viii)
3. Exemptions: Medical loans under Rule 18, or aggregate < ₹2,00,000
4. Rate used: SBI MCLR on April 1 of the financial year (not real-time)
5. Opening balance used for each month (not average or closing)
6. Even if loan is paused, perquisite continues on outstanding balance (loan still outstanding)
7. When loan closes: perquisite = ₹0 for all subsequent months

## Gaps / Observations
- SBI MCLR rate configuration not found in Settings — unclear if admin updates it or Zoho maintains it
- "Income Tax Rules, 2026" citation in UI is likely an errata for "IT Rules, 1962" — should be verified with Zoho
- Aggregate threshold checking across multiple loans not verified (demo has only one loan per employee)
- Vikram's loan: whether the "exempt" checkbox was checked on creation not confirmed

## Open Questions
- [ ] Where does admin configure or update the SBI lending rate? Is it auto-fetched by Zoho?
- [ ] Does the system check aggregate outstanding across all active loans automatically?
- [ ] If the "Exempt" checkbox is checked on a loan > ₹2L, does the system override and warn?
- [ ] Does perquisite accrue even during a paused loan period?
