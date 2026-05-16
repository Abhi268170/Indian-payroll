# UF-40: Verify TDS — May 2026 Pay Run

**Module:** Pay Runs > Summary > Taxes & Deductions / Employee > Investments & Proofs
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run, Arjun Mehta (EMP001), Priya Sharma (EMP002)
**App State Before:** Pay Run status = PAID; IT Declaration locked for Arjun Mehta; no declaration submitted

## Steps Executed
1. Navigate to pay run Taxes & Deductions tab → Income Tax row = ₹0
2. Open Arjun Mehta detail panel → Income Tax = ₹0
3. Open Priya Sharma detail panel → Income Tax = ₹0
4. Navigate to `#/people/employees/3848927000000032948/investments-and-proofs`
5. Observe IT Declaration state for Arjun Mehta: LOCKED, no declaration submitted
6. Compute expected TDS manually and compare

## TDS in May 2026 Pay Run

| Employee | Gross Monthly | Annual CTC | Income Tax Deducted | Expected? |
|----------|--------------|------------|---------------------|-----------|
| Arjun Mehta | ₹70,000 | ₹8,40,000 | ₹0.00 | Needs investigation |
| Priya Sharma | ₹22,000 | ₹2,64,000 | ₹0.00 | Correct ✓ |

## Priya Sharma — TDS ₹0 (Correct)

Annual CTC: ₹2,64,000
New regime FY2026-27:
- Standard Deduction: ₹75,000
- Taxable income: ₹2,64,000 − ₹75,000 = ₹1,89,000
- Slab: ₹0–4,00,000 = 0% under new regime (FY2026-27 slabs: up to ₹4L = 0%)
- Tax: ₹0
- TDS: ₹0 ✓

Note: FY2026-27 new regime slabs (post-Budget 2025):
| Slab | Rate |
|------|------|
| Up to ₹4,00,000 | 0% |
| ₹4,00,001 – ₹8,00,000 | 5% |
| ₹8,00,001 – ₹12,00,000 | 10% |
| ₹12,00,001 – ₹16,00,000 | 15% |
| ₹16,00,001 – ₹20,00,000 | 20% |
| ₹20,00,001 – ₹24,00,000 | 25% |
| Above ₹24,00,000 | 30% |

## Arjun Mehta — TDS ₹0 (Requires Explanation)

Annual CTC: ₹8,40,000
New regime FY2026-27:
- Standard Deduction: ₹75,000
- Taxable income: ₹8,40,000 − ₹75,000 = ₹7,65,000
- Tax calculation:
  - ₹0–4L → ₹0
  - ₹4L–7.65L = ₹3,65,000 @ 5% = ₹18,250
  - Total tax: ₹18,250
- Section 87A rebate: Available if total tax ≤ ₹25,000 AND income ≤ ₹7,00,000 (new regime)
- Arjun's income ₹7,65,000 > ₹7,00,000 → 87A rebate NOT available
- Health & Education Cess: 4% × ₹18,250 = ₹730
- Total annual tax: ₹18,250 + ₹730 = ₹18,980
- Monthly TDS: ₹18,980 / 12 = ₹1,582

**Expected TDS per month: ₹1,582**
**Actual TDS in May: ₹0**

## Root Cause: IT Declaration LOCKED, No Declaration Submitted

From the investments-and-proofs page:
- IT Declaration status: "IT Declaration submission is locked for this employee"
- Message: "You can either allow the employee to submit IT Declaration through the portal or submit it on their behalf"
- CTA: "Submit Declaration" → `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions`
- Period shown: 2026-27
- No declaration has been submitted for FY2026-27

The URL parameter `?tax_regime=with_exemptions` indicates the declaration form defaults to old regime (with exemptions). This is inconsistent with the system being "new regime only" per CLAUDE.md. Need to investigate further.

## Possible Reasons TDS = ₹0 Despite Liability

Three hypotheses:
1. **No declaration submitted → system defaults to ₹0 TDS until a declaration is filed.** This is a design choice — some payroll systems hold TDS at ₹0 until declaration received. Common in first month(s) of fiscal year.
2. **Pay run was processed in a state where TDS computation was deferred.** The pay run was created during trial — the system may not compute TDS until certain settings are activated (e.g., tax details configured).
3. **Settings > Taxes hasn't been configured.** The "Tax Details" link at `#/settings/taxes` may have a switch to enable TDS computation.

## IT Declaration UI (Arjun Mehta)

| Element | Value |
|---------|-------|
| Page URL | `#/people/employees/3848927000000032948/investments-and-proofs` |
| Tabs | IT Declaration | Proof Of Investments |
| Active tab | IT Declaration |
| Period selector | "Period: 2026-27" (dropdown button) |
| State | LOCKED — "IT Declaration submission is locked for this employee" |
| Admin CTA | "Submit Declaration" (on behalf of employee) |
| Declaration URL | `#/people/employees/{id}/investment-declaration/new?tax_regime=with_exemptions` |

## TDS Sheet Button in Pay Run

In the Employee Summary tab, each paid employee row has a "TDS Sheet" button (View). This opens a per-employee TDS computation document for the month. Content not explored in this session.

## Statutory References
- TDS on Salary: Section 192 of Income Tax Act, 1961
- Employer must deduct TDS at average rate computed on estimated annual income
- If no declaration submitted, employer computes TDS on gross salary without deductions
- New regime (Section 115BAC) is default for FY2026-27 unless employee opts out explicitly
- 🔴 TDS ₹0 for Arjun with ₹8.4L salary while no declaration submitted is non-compliant — employer is required to deduct TDS even in absence of declaration

## Gaps / Observations
- 🔴 TDS not being computed for Arjun Mehta (₹8,40,000 salary, ~₹1,582/month expected)
- 🔴 System showing TDS ₹0 when no IT declaration submitted — this is incorrect; TDS should be computed on gross without deductions if no declaration exists
- `?tax_regime=with_exemptions` in "Submit Declaration" URL suggests old regime is available despite new-regime-only intended scope
- No TDS Sheet was opened to verify the per-employee computation — this is an open investigation item
- "Settings > Taxes > Tax Details" page at `#/settings/taxes` not yet investigated — may have TDS enable/disable switch

## Open Questions
- [ ] What does Settings > Tax Details (`#/settings/taxes`) configure? Does it have a TDS enable switch?
- [ ] What is inside the "TDS Sheet" (View button in Employee Summary)?
- [ ] Does Zoho Payroll require IT declaration submission before computing any TDS?
- [ ] Why does "Submit Declaration" link use `?tax_regime=with_exemptions` (old regime) when only new regime is configured?
