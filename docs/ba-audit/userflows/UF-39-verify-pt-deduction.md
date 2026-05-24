# UF-39: Verify Professional Tax Deduction (May 2026 Pay Run)

**Module:** Pay Runs > Summary / Settings > Statutory Components > Professional Tax
**Tested:** 2026-05-16
**Mock Data Used:** May 2026 Regular Pay Run, Kerala PT configuration
**App State Before:** Pay Run status = PAID

## Steps Executed
1. Navigate to `#/settings/statutory-details/list/pt`
2. Open "View Tax Slabs" modal for Head Office
3. Capture Kerala PT slab table and deduction cycle
4. Cross-reference with May 2026 pay run Taxes & Deductions tab (KL PT = ₹0)
5. Examine each employee detail panel (both show KL PT ₹0.00)

## PT Configuration (Settings > Statutory Components > Professional Tax)

| Setting | Value |
|---------|-------|
| Work Location | Head Office |
| PT Number | (not entered — blank, "Update PT Number" button shown) |
| State | Kerala |
| Deduction Cycle | Half Yearly |
| Effective From | 01/04/2026 |

## Kerala PT Slabs (Half-Yearly, FY2026-27)

| Half-Yearly Gross Salary (₹) | Half-Yearly Tax Amount (₹) |
|-------------------------------|----------------------------|
| 1 – 11,999 | 0 |
| 12,000 – 17,999 | 320 |
| 18,000 – 29,999 | 450 |
| 30,000 – 44,999 | 600 |
| 45,000 – 99,999 | 750 |
| 1,00,000 – 1,24,999 | 1,000 |
| 1,25,000 – 99,99,99,999 | 1,250 |

Note: Kerala PT is assessed on HALF-YEARLY gross salary (sum of 6 months' gross), and the full slab amount is deducted in ONE month of the half year. This is standard Kerala PT behaviour under the Kerala Panchayat Raj Act.

## Key Finding: Why PT = ₹0 in May 2026

**Deduction Cycle = Half Yearly** is the reason.

Kerala collects PT twice per year:
- First half: April–September → tax deducted in September
- Second half: October–March → tax deducted in March

May 2026 is in the first half (April–September) but is NOT the deduction month. PT will be deducted in September 2026 and March 2027 only.

This is CORRECT behaviour per Kerala PT statute — ₹0 in May is expected and compliant.

## Expected PT Calculation for September 2026 (Arjun Mehta)

If salary remains ₹70,000/month (pre-revision) for April–September:
- Half-yearly gross (6 months) = ₹70,000 × 6 = ₹4,20,000
- But PT is assessed on monthly gross for the half, not cumulative sum — Kerala uses MONTHLY gross to determine slab, then deducts the full period amount once.
- Arjun monthly gross: ₹70,000 (or ₹65,484 in May due to LOP)
- Monthly gross > ₹30,000/month → half-yearly equivalent > ₹1,25,000 → PT = ₹1,250 per half-year
- September deduction: ₹1,250

For Priya Sharma:
- Monthly gross: ₹22,000
- Half-yearly equivalent: ₹22,000 × 6 = ₹1,32,000 → slab ₹1,25,000–∞ → PT = ₹1,250 per half-year
- September deduction: ₹1,250

Note: The actual slab interpretation for Kerala Half-Yearly PT may be based on monthly salary assessed against monthly slab equivalents rather than cumulative 6-month total. The system may calculate differently — to confirm, a September 2026 pay run would need to be run.

## PT Statutory Information
- Kerala PT is governed by the Kerala Panchayat Raj (Professional Tax) Rules
- Maximum PT in Kerala: ₹2,500/year (₹1,250 per half year)
- PT is employee-borne (no employer contribution)
- PT paid under Section 80(1)(b) of Income Tax Act is deductible from gross income under the new regime
- Under new tax regime (FY2026-27): PT deduction is NOT allowed as a standard deduction (only the flat ₹75,000 standard deduction applies)

## Gaps / Observations
- PT Number field is blank — "Update PT Number" button is present but PT Number has not been entered. This is a statutory compliance gap: challan generation requires a valid PT enrollment number.
- 🔴 No PT Number entered — PT challan cannot be filed without this
- "Revise" button visible next to tax slabs — allows overriding the state-provided slab table. Custom slabs should only be set if the state has notified different rates for a specific period.
- Deduction cycle "Half Yearly" is not configurable by admin — it mirrors the Kerala statute. However the system allows "Revise" which could corrupt the cycle configuration.
- No indicator in pay run summary of which month PT will be deducted — users may be confused by persistent ₹0 PT lines.

## Employee PT Status
| Employee | PT Enabled | Reason for ₹0 in May |
|----------|-----------|----------------------|
| Arjun Mehta | Yes (PT Enabled on profile) | Half-yearly cycle — deducts in Sep & Mar only |
| Priya Sharma | Yes (PT Enabled on profile) | Same |
| Vikram Nair | Yes (PT Enabled) | Skipped — onboarding incomplete |
| Aisha Khan | Yes (PT Enabled) | Skipped |
| Rahul Desai | Yes (PT Enabled) | Skipped |
