# UF-21: Salary Revision — Increment (Arjun Mehta)

**Module:** Employees → Employee Profile → Salary Details → Revise
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001; existing revision ₹8,40,000 → ₹9,45,000 already present

## Context

When navigating to Arjun Mehta's Salary Details to test the revision flow, a revision was already present in the system. The existing revision details were documented from the revision detail page. The "Create new revision" flow was not re-executed to avoid duplicate revisions.

## Salary Revision — Detail View

**Page title:** "Salary Revision for Arjun Mehta"
**Actions available:** Delete | Edit | Close (X)

### Revision Summary Header

| Field | Value | Notes |
|-------|-------|-------|
| Previous CTC | ₹8,40,000.00 | Pre-revision annual CTC |
| New CTC | ₹9,45,000.00 | Post-revision annual CTC |
| % Change | 13% (green upward arrow) | Auto-calculated; shown inline with New CTC |
| Effective From | June, 2026 | Month the new salary takes effect |
| Payout Month | June, 2026 | Month employee first receives revised salary |

### Revised Salary Structure

| Salary Components | Monthly Amount | Annual Amount |
|-------------------|---------------|---------------|
| **Earnings** | | |
| Basic (57.14% of CTC) | ₹44,998.00 | ₹5,39,976.00 |
| House Rent Allowance (40.00% of Basic Amount) | ₹17,999.00 | ₹2,15,988.00 |
| Fixed Allowance | ₹15,753.00 | ₹1,89,036.00 |
| **Cost to Company** | **₹78,750.00** | **₹9,45,000.00** |

### Calculation Verification
- Monthly CTC = ₹9,45,000 / 12 = ₹78,750 ✓
- Basic: 57.14% × ₹78,750 = ₹44,998.35 → ₹44,998.00 ✓
- HRA: 40.00% × ₹44,998 = ₹17,999.20 → ₹17,999.00 ✓
- Fixed Allowance: ₹78,750 − ₹44,998 − ₹17,999 = ₹15,753.00 ✓
- Total: ₹78,750 × 12 = ₹9,45,000 ✓

**Observation:** Component percentages (57.14% Basic, 40% HRA) are preserved from the original salary structure. Only the CTC amount changes; ratios stay the same.

## Salary Revision — Create Flow (Inferred from "Revise" Button)

From Salary Details tab → "Revise" button → opens revision creation form.

### Expected Inputs (not directly observed but inferred from revision detail)
| Field | Type | Notes |
|-------|------|-------|
| New Annual CTC | Number (₹) | Employee enters revised CTC |
| Effective From | Month/Year picker | Month salary revision takes effect |
| Payout Month | Month/Year picker | May differ if arrears payout is separate |
| Arrears handling | Checkbox/Option | Whether to pay arrears for gap months (not confirmed) |

## Info Banner on Salary Details Tab

When a pending revision exists:
> "The revised salary amount will be reflected in salary details upon the completion of June, 2026 pay run. View Details"

- "View Details" links to the revision detail page documented above
- Banner disappears once the payout month's pay run is finalized

## Actions on Revision Detail Page

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Edit | Button | Revision not yet paid | Opens revision edit form (same as create form, pre-filled) |
| Delete | Button | Revision not yet paid | Presumably shows confirmation dialog; deletes revision |
| Close (X) | Button | Any time | Returns to Salary Details tab |

## Business Rules

1. **One active pending revision at a time** — System appeared to have one pending revision. Whether multiple pending revisions can coexist is not confirmed.

2. **Revision becomes active after pay run** — The revised salary is NOT applied immediately. It activates when the pay run for the payout month is completed. Until then, the previous CTC remains "current."

3. **Component ratios preserved** — When CTC changes, all component percentages stay the same. Fixed Allowance recalculates as residual at new CTC level.

4. **% Change indicator** — System calculates and shows percentage change inline (13% increase in this case). Green up-arrow for increase, presumably red down-arrow for decrease.

5. **Effective From = Payout Month** (in this case both June 2026) — May support split where effective date precedes payout (arrears scenario). Not confirmed.

## State Machine

```
[No Revision] → Revise button → [Pending Revision] 
[Pending Revision] → Pay Run Finalized → [Active / Historical]
[Pending Revision] → Delete → [No Revision]
[Pending Revision] → Edit → [Pending Revision (updated)]
```

## Data Relationships
- Salary Revision → Employee (M:1)
- Salary Revision → Salary Components (snapshot at revision creation time)
- Salary Revision → Pay Run (activated by specific pay run)

## Navigation
- Entry: Salary Details tab → "Revise" button
- Revision detail: `#/employees/{id}/salary-revision/{revision-id}` (exact route estimated)
- "View Details" from info banner → revision detail
- Post-delete: returns to Salary Details tab (no pending revision)

## Screenshots
- [Salary revision detail page](../screenshots/UF-21-salary-revision-details.png)
- [Salary details with pending revision banner](../screenshots/UF-15-arjun-salary-details.png)

## Gaps / Observations
- No arrears calculation visible in the revision detail — whether the system auto-calculates arrears for salary changes effective in past months is not confirmed
- Edit/Delete buttons visible but not exercised — edit flow fields not documented
- "Payout Month" vs "Effective From" distinction unclear when they can differ
- No approval workflow observed for salary revision — revision appears to be directly saveable by admin without approval chain (unlike some enterprise payroll systems)
- Revision % change is display-only; no field for admin to input "% increase" and compute new CTC (must calculate manually)
