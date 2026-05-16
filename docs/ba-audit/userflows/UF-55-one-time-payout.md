# UF-55: One-Time Payout

**Module:** Pay Runs > New > One Time Payout
**Tested:** 2026-05-16
**Mock Data Used:** Clicked "One Time Payout" — first option in the dropdown
**App State Before:** No active pay runs; May 2026 = PAID

## Steps Executed
1. Navigate to `#/payruns`
2. Click "New" dropdown
3. Observe listbox shows: "One Time Payout" (active/first) and "Off Cycle Payrun"
4. "One Time Payout" was the first focused item in the dropdown
5. Proceeded to click "Off Cycle Payrun" for UF-52 — One Time Payout flow not completed

## Known from UI: Pay Run Type Options
From the "New" button dropdown on `#/payruns`:
| Position | Label | Purpose |
|----------|-------|---------|
| 1 | One Time Payout | Ad-hoc single-payment for specific purpose (e.g., bonus, gift, incentive) |
| 2 | Off Cycle Payrun | Mid-month supplementary pay run |

## One-Time Payout — Expected Flow (Not Fully Tested)

A One-Time Payout in Zoho Payroll typically:
1. Prompts for: Employee selection, payout component (e.g., "Performance Bonus"), amount, pay date
2. Creates a standalone pay run entry in Payroll History
3. Generates a payslip/advice for the one-time amount
4. Tax treatment: Added to YTD income for TDS computation; may require IT declaration update

## Statutory Treatment of One-Time Payouts
- Performance Bonus: Taxable as salary income under Section 17(1)
- Gift/perquisite: If given in kind (voucher, gift card) — taxable under Section 17(2) if > ₹5,000
- Ex-gratia: Taxable unless it is retrenchment compensation or VRS eligible for exemption
- Joining Bonus: Taxable as salary; if employee leaves within 1 year and returns bonus, deductible

## Gaps / Observations
- One-Time Payout flow not tested — need to navigate and fill the form
- Not clear if One-Time Payout has a "Component Type" selector or if it defaults to a specific component
- Whether One-Time Payout creates a separate pay run ID or is attached to an existing pay run is unknown
- 🟡 Mark as requiring future session investigation
