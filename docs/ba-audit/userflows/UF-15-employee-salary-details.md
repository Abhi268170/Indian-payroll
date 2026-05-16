# UF-15: Employee Salary Details — Arjun Mehta

**Module:** Employees → Employee Profile → Salary Details tab
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001, CTC ₹8,40,000/yr (actual system value)

## Steps Executed
1. Navigated to Arjun Mehta employee profile
2. Clicked "Salary Details" tab
3. Observed Salary Details section and Salary Structure breakdown
4. Noted pending revision info banner
5. Scrolled to see Perquisites section heading

## Salary Details Tab — Layout

The tab contains three sections visible:
1. **Salary Details** — top card with CTC summary and action buttons
2. **Salary Structure** — component breakdown table
3. **Perquisites** — (visible heading; not scrolled further)

## Salary Details Section (Top Card)

| Field | Value | Notes |
|-------|-------|-------|
| Annual CTC | ₹8,40,000.00 per year | Current active CTC |
| Monthly CTC | ₹70,000.00 per month | Annual / 12 |

### Info Banner
"The revised salary amount will be reflected in salary details upon the completion of June, 2026 pay run. View Details"
- "View Details" is a hyperlink → navigates to the salary revision detail page
- Banner appears because a pending salary revision exists (₹8,40,000 → ₹9,45,000, effective June 2026)

### Action Buttons
| Button | Notes |
|--------|-------|
| Revise | Opens salary revision flow (UF-21) |
| ... (more) | Dropdown — likely Edit Salary Details, Download, etc. (not explored) |

## Salary Structure Table

| Salary Components | Monthly Amount | Annual Amount |
|-------------------|---------------|---------------|
| **Earnings** | | |
| Basic (57.14% of CTC) | ₹39,998.00 | ₹4,79,976.00 |
| House Rent Allowance (40.00% of Basic Amount) | ₹15,999.00 | ₹1,91,988.00 |
| Fixed Allowance | ₹14,003.00 | ₹1,68,036.00 |
| **Cost to Company** | **₹70,000.00** | **₹8,40,000.00** |

### Key Observations on Salary Structure

1. **Basic at 57.14% of CTC** — differs from template "Standard-Exec" which uses 50.00%. This employee's structure was created independently with a different Basic percentage.

2. **HRA at 40.00% of Basic** — unlike Standard-Exec template (50% of Basic). 40% HRA is appropriate for non-metro cities (Thiruvananthapuram, Kerala). 50% HRA applies to metro cities per Income Tax rules.

3. **Fixed Allowance as residual** — ₹14,003 = ₹70,000 − ₹39,998 − ₹15,999 = ₹14,003. Confirmed Fixed Allowance absorbs residual CTC.

4. **Rounding artifact** — Basic = 57.14% × ₹70,000 = ₹39,998. Not a clean 50% or 40% — suggests the salary was entered as a custom CTC with these custom percentages, not via Standard-Exec template.

5. **No Special Allowance** — despite Special Allowance being created as a component, it is not in this employee's salary structure. Template assignment at employee creation determines which components appear.

### Calculation Verification
- Basic: 57.14% × 70,000 = 39,998.00 ✓
- HRA: 40.00% × 39,998 = 15,999.20 → rounded to ₹15,999.00 ✓
- Fixed Allowance: 70,000 − 39,998 − 15,999 = 14,003.00 ✓
- Total: ₹70,000.00/month = ₹8,40,000.00/year ✓

## Perquisites Section
Section heading visible below Salary Structure. Contents not fully documented (scrolling not captured). Perquisites in Indian payroll context: car, accommodation, ESOP, club membership, etc. — taxable as per Income Tax Act Section 17(2).

## Business Rules

- Salary Details tab is read-only for CTC and structure; edit via "Revise" button creates a salary revision (not direct edit)
- When pending salary revision exists, an info banner shows effective date and link to revision details
- Salary structure component percentages are set at employee-salary creation time and shown as labels "(X% of CTC)" or "(X% of Basic Amount)"
- Fixed Allowance always appears as residual — no percentage label shown for it (unlike Basic and HRA)
- HRA percentage convention: 50% for metro, 40% for non-metro — this system uses 40% for Head Office (Thiruvananthapuram, Kerala) — correct per statute

## Data Relationships
- Employee → Salary Structure (1:1 active structure)
- Salary Structure → Salary Components (1:N)
- Salary Structure → Salary Revision history (1:N)
- Salary template → Employee (N:M, template is copied at assignment time)

## State Machine
- Active salary: shown in Salary Details card
- Pending revision: shown as info banner; becomes active after payout month's pay run completes
- Revision states: Pending → Active (after pay run) | Deleted

## Navigation
- Entry: Employee profile → "Salary Details" tab
- "View Details" link → Salary Revision detail page
- "Revise" button → Salary revision creation flow

## Screenshots
- [Arjun Mehta Salary Details tab](../screenshots/UF-15-arjun-salary-details.png)

## Gaps / Observations
- HRA at 40% of Basic is correct for non-metro (Kerala) — well-implemented
- No "Effective From" date shown on current salary structure (only the pending revision has Effective From)
- Perquisites section content not documented — requires follow-up
- "..." dropdown actions on Salary Details card not explored
- No visible "Tax Regime" indicator on Salary Details tab — TDS regime may be on Investments tab only
