# UF-22: Salary Revision with Arrears

**Module:** Employees > Salary Details
**Tested:** 2026-05-16
**Mock Data Used:** Arjun Mehta EMP001 — existing revision (ID: 3848927000000034251)
**App State Before:** Existing revision from ₹8,40,000 → ₹9,45,000 effective June 2026, payout June 2026

## Steps Executed
1. Navigate to Arjun's Salary Details: `#/people/employees/3848927000000032948/salary-details`
2. Observed salary revision notice banner on salary details page
3. Clicked "View Details" → navigated to revision detail page
4. Clicked "Edit" on revision → navigated to revision edit form
5. Attempted to change "Revised Salary effective from" to "Jan 2026" (backdated)
6. Observed calendar picker and helper text changes
7. Observed "Leave this page?" confirmation dialog

## Existing Salary Revision Details

| Field | Value |
|-------|-------|
| Previous CTC | ₹8,40,000.00 per year |
| New CTC | ₹9,45,000.00 per year |
| Increase | 13% |
| Effective From | June 2026 |
| Payout Month | June 2026 |

**Revised Salary Structure (₹9,45,000 CTC):**

| Component | Calculation | Monthly | Annual |
|-----------|-------------|---------|--------|
| Basic | 57.14% of CTC | ₹44,998 | ₹5,39,976 |
| HRA | 40% of Basic | ₹17,999 | ₹2,15,988 |
| Fixed Allowance | CTC - others | ₹15,753 | ₹1,89,036 |
| **CTC** | | **₹78,750** | **₹9,45,000** |

**Math verification:**
- Basic: ₹9,45,000 × 57.14% / 12 = ₹44,998.50 ≈ ₹44,998 ✓
- HRA: ₹44,998 × 40% = ₹17,999.20 ≈ ₹17,999 ✓
- Fixed: ₹78,750 - ₹44,998 - ₹17,999 = ₹15,753 ✓

## Salary Revision Edit Form — All Fields

**URL:** `#/people/employees/{id}/salary-revision/{revisionId}/edit`

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| Salary Templates | Dropdown | No | "Select" | Pulls from configured salary templates |
| Revision Type | Radio | Yes | "Enter new CTC amount" | "Revise CTC by percentage" OR "Enter the new CTC amount below" |
| Revision Percentage | Spinbutton | Conditional | Disabled | Active only when "by percentage" selected |
| Revised Annual CTC | Spinbutton (₹) | Yes | Current value | Numeric, ₹ prefix, "per year" suffix |
| Basic % of CTC | Spinbutton | Yes | 57.14 | Editable percentage |
| HRA % of Basic | Spinbutton | Yes | 40.00 | Editable percentage |
| Fixed Allowance | Read-only | N/A | Auto-calculated | Monthly CTC - Sum of all other components |
| Revised Salary effective from | Textbox (month picker) | Yes | Current revision date | Month/year picker |
| Payout Month | Textbox (month picker) | Yes | Same as effective date | Month/year picker |

## Backdated Revision Behavior — Key Observations

When "Revised Salary effective from" is typed as "Jan 2026" (a past month):
1. **Helper text updates immediately:** "The revised salary for Arjun will be applicable from January, 2026."
2. **Calendar picker opens:** Shows 2026 calendar — Jan through May 2026 are **greyed out** (not clickable via calendar), only Jun 2026+ are selectable
3. **The Payout Month field still shows Jun 2026** with helper text: "The revised salary amount will be paid out in June, 2026, along with the arrears (if any)."

**Arrears Note (verbatim from page):**
> "Note: Zoho Payroll will automatically calculate any arrears in the salary and process them in the payout month, eliminating the need for manually adding arrear components."

## Arrears Logic (Inferred)

When effective date is set to a month earlier than payout month:
- System detects gap between effective date and payout month
- Arrears = (New monthly salary - Old monthly salary) × number of months in the gap
- Arrears are automatically added to the payout month's pay run
- No separate "arrears component" needs to be manually added

**Example (if backdated to Jan 2026, payout June 2026):**
- Old monthly: ₹70,000
- New monthly: ₹78,750
- Difference: ₹8,750/month
- Gap: Jan–May 2026 = 5 months
- Arrears = ₹8,750 × 5 = ₹43,750 (would appear in June 2026 pay run)

## Unsaved Changes Guard

When navigating away from the edit form with unsaved changes, a browser-native dialog appears:
- **Heading:** (modal dialog)
- **Message:** "You might have some unsaved changes. Are you sure you want to leave this page?"
- **Buttons:** "Stay on this page" (primary/active) | "Leave this page"

## Actions on Revision Detail Page

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Delete | Button | Revision exists, not yet processed | Deletes revision record |
| Edit | Link → edit page | Revision exists | Opens edit form |
| Close | Link | Always | Returns to Salary Details tab |

## Salary Details Page — Revision Pending Banner

On the Salary Details tab, when a revision is pending (not yet processed):
- Banner shows: "The revised salary amount will be reflected in salary details upon the completion of June, 2026 pay run."
- "View Details" link to revision detail page

## Business Rules
- Calendar picker constrains to current month onwards — past months cannot be selected via the UI picker (only via text input)
- Text input accepts past dates ("Jan 2026") but only future months are shown as clickable in the calendar
- Arrears are auto-computed and added to payout month — no manual arrear component needed
- Only one pending revision allowed at a time (the "Revise" and dropdown buttons on salary details suggest one pending revision exists)
- Revision is applied only after the payout month's pay run is completed

## Cross-Module Effects
- Pending revision visible on Salary Details page as a banner
- Payout month's pay run will include arrears as an auto-computed line item
- Salary structure on the profile page still shows OLD values until revision is processed

## Gaps / Observations
- Calendar picker blocks past months but text input accepts them — inconsistency between picker and input validation; effective from Jan 2026 was accepted without an error
- No warning when effective-from is set to months already processed by a pay run (arrears for those months cannot be recalculated after finalization)
- No preview of arrears amount in the form — user cannot see "₹43,750 arrears will be created" before saving
- No approval workflow triggered for salary revision in current config (Simple Approval with no approvers assigned)
