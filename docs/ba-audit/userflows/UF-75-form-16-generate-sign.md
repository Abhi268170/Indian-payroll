# UF-75: Form 16 — Generate and Sign

**Module:** Taxes & Forms > Form 16 > Steps 2 & 3
**Tested:** 2026-05-16
**Mock Data Used:** Demo org; Tax Deductor not configured — generation blocked
**App State Before:** Form 16 page showing blocking error; Steps 2 & 3 disabled

## Steps Executed
1. Observed Form 16 page with 4-step wizard
2. Steps 2 (Generate) and 3 (Sign) are disabled pending Step 1 (Upload Part A) completion
3. Documented expected flow from UI structure and statutory requirements

---

## Generate Step (Step 2) — Expected Flow

### Pre-conditions
- Tax Deductor configured with TAN
- Form 24Q filed for all quarters of the financial year
- Part A uploaded for all employees (Step 1 complete)

### Generation Process
1. Admin clicks "Generate" on the Form 16 page
2. System triggers Part B generation for all employees:
   - Computes salary breakup per quarter (Basic, HRA, Allowances, Perquisites)
   - Computes Chapter VI-A deductions (80C, 80D, etc. — from IT declarations)
   - Computes taxable income and tax liability
   - Applies rebate under Section 87A if applicable
   - Shows YTD TDS deducted vs liability
3. Part B is merged with uploaded Part A PDF to produce combined Form 16
4. Generation is per financial year — one Form 16 per employee covering April–March
5. Status shows: N employees with Form 16 generated / M pending

### Part B Content (Statutory per Rule 31 Annexure II)
| Section | Content |
|---------|---------|
| Employer Details | Name, Address, TAN, PAN |
| Employee Details | Name, PAN, Period of employment |
| Gross Salary | Total salary u/s 17(1) + Perquisites u/s 17(2) + Profits in lieu u/s 17(3) |
| Less: Allowances exempt u/s 10 | HRA, LTA, etc. |
| Net Salary (Gross − Exemptions) | |
| Less: Deductions u/s 16 | Standard Deduction ₹75,000 (FY2025-26 onward), Entertainment Allowance, PT |
| Income from Salary (Taxable) | |
| Add: Other income declared by employee | |
| Gross Total Income | |
| Less: Chapter VI-A deductions | 80C, 80D, 80G, 80TTA, etc. |
| Total Income (Taxable) | |
| Tax on Total Income | Per new regime slabs |
| Less: Rebate u/s 87A | If total income ≤ ₹7,00,000 in new regime |
| Surcharge | If applicable (income > ₹50L) |
| Health & Education Cess | 4% |
| Total Tax Payable | |
| Less: TDS Deducted | Month-wise in the financial year |
| Tax Payable / Refundable | Balance |

---

## Sign Step (Step 3) — Expected Flow

### Signing Methods
| Method | Notes |
|--------|-------|
| Digital Signature Certificate (DSC) | USB token or cloud DSC; legally valid under IT Act |
| Manual Signature | Print and physically sign; scan and upload |
| Aadhar-based eSign | Available in some platforms; not confirmed in Zoho |

### Zoho Payroll Signing Behavior
- If DSC configured: Admin clicks "Sign", DSC-based signing happens in-browser
- If manual: Zoho generates unsigned PDFs for download → admin signs → Form 16 marked as signed manually
- Unsigned Form 16 should not be issued to employees (invalid for tax purposes)

### Authorized Signatory Requirements
- Must be the person responsible for deducting TDS (as per company authorization)
- Their PAN and designation must match what is in Tax Deductor configuration
- Designation typically: Director, CFO, GM Finance, or designated TDS-responsible officer

---

## Statutory Context

| Item | Requirement |
|------|------------|
| Issuance deadline | 15th June after FY end |
| Penalty for non-issue | Section 272A: ₹100/day for each failure |
| Form 16 without DSC | Accepted if manually signed by authorized signatory |
| Revised Form 16 | Issued if TDS is corrected after filing revised 24Q |

---

## Gaps / Observations
- Generation blocked due to Tax Deductor not configured — actual Generate UI not tested
- 🔴 Cannot verify if Zoho generates Form 16 for employees with PAN = "-" or blocks it
- Sign step method (DSC vs manual) not observed in active state
- No test of multi-company Form 16 (branch/division scenario)

## Open Questions
- [ ] Does generation fail for individual employees with missing PAN, or generate all except PAN-missing employees?
- [ ] Can admin regenerate Form 16 after a correction (e.g., revised 24Q was filed)?
- [ ] Does Zoho support cloud DSC (e.g., eMudhra, Sify)?
- [ ] Is there a bulk signing option for large employee counts?
