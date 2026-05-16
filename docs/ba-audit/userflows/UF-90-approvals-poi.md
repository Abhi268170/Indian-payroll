# UF-90: Approvals — Proof of Investments (POI)

**Module:** Approvals > Proof of Investments
**Tested:** 2026-05-16
**URL:** `#/approvals/proof-of-investment`
**Mock Data Used:** Demo org; IT Declaration locked; no POI submitted
**App State Before:** IT Declaration locked; employee portal access active

---

## POI Approvals Page

### Navigation
Sidebar > Approvals (expand) > Proof of Investments

### Page Layout
- Header action bar: "All Investments" dropdown filter, info bar, action buttons
- Filter bar: Fiscal Year, Tax Regime, Employees
- Content area: Empty state (no POI submissions yet)

---

## Header Elements

### "All Investments" Dropdown Filter
Button with dropdown — filters the POI list by investment category:
- All Investments
- (Individual investment categories: 80C, 80D, HRA, etc.)

### Info Bar
"2 employee(s) yet to submit POI" with "View" button
- Shows count of employees who have NOT submitted POI
- "View" → filters list to show those employees or navigates to employee list

### Action Buttons
| Button | Description |
|--------|-------------|
| "Show dropdown menu" | More options (send reminder, export?) |
| Filter icon | Filter/sort the POI list |
| "Instant Helper" | Context help |

---

## Filter Bar

| Filter | Type | Options |
|--------|------|---------|
| Fiscal Year | Combobox | FY 2026-2027 (default), prior years |
| Tax Regime | Combobox | Select Tax Regime (All / New / Old) |
| Employees | Combobox | Select an Employee (specific employee) |

---

## Empty State

"This is your space to review your employees' investment proofs!"

Body text: "Your employees can submit their investment proofs through the employee portal once you enable the option in **Settings > Preferences**."

**Root cause of empty state:** IT Declaration is LOCKED. Employees cannot submit POI while locked.

---

## POI Workflow (Expected When Unlocked)

### Step 1: Admin Releases IT Declaration
Settings > Preferences > IT Declaration > Release IT Declaration

### Step 2: Employee Submits IT Declaration
Employee portal → Tax Declaration → Fill investment amounts for:
- 80C: LIC, EPF, PPF, ELSS, NSC, housing loan principal, tuition fees, etc.
- 80D: Medical insurance (employer and individual)
- 80G: Donations
- HRA: House rent paid (if applicable)
- Other deductions

### Step 3: Admin Opens POI Submission Window
Settings > Preferences > IT Declaration → Enable POI submission

### Step 4: Employee Submits POI (Supporting Documents)
Employee portal → Tax Declaration > POI → Upload documents proving declared investments:
- LIC premium receipt
- PPF statement
- ELSS statement
- Rent receipts (for HRA claims)

### Step 5: Admin Reviews POI (This Page)
Approvals > Proof of Investments → Lists all submitted POI for review

**Per submission view:**
| Element | Description |
|---------|-------------|
| Employee Name | Who submitted |
| Investment Category | 80C / 80D / HRA / etc. |
| Declared Amount | Amount employee declared |
| Uploaded Document | Download link for proof |
| Approved Amount | Admin enters (≤ declared) |
| Actions | Approve / Reject |

### Step 6: Admin Approves / Rejects
**Approve:**
- Approved amount may differ from declared (admin can reduce if proof insufficient)
- TDS recalculates based on approved amounts
- Employee is notified

**Reject:**
- Reason required (optional text field)
- Employee can resubmit with correct documentation
- TDS reverts to without-deduction computation

---

## Approval Impact on TDS

When admin approves ₹1,00,000 u/s 80C:
- Taxable income reduces by ₹1,00,000 (old regime only — NOT applicable in new regime)
- TDS is recalculated for remaining months of FY

**New Regime Note:** In new regime, 80C/80D deductions are NOT available. POI approval only matters for employees who have opted for old regime (if "Allow regime switch" is enabled — confirmed CHECKED in demo org).

---

## Business Rules
1. POI submission requires IT Declaration to be unlocked
2. POI submission window is separate from IT Declaration window (admin controls both)
3. Admin can approve partial amounts (< declared)
4. Approved amounts feed directly into TDS computation
5. In new regime: POI is irrelevant (no deductions available) — only old regime employees need POI
6. Rejected POI → employee must resubmit within the POI window period
7. Final TDS computation uses approved (not declared) amounts

## Gaps / Observations
- No POI submissions to test with (IT Declaration locked)
- "2 employee(s) yet to submit POI" count visible even though locked — suggests system tracks submission status
- 🔴 No POI-specific unlock mechanism observed — need to confirm if POI window is separate from IT Declaration lock

## Open Questions
- [ ] Is the POI submission window a separate window from the IT Declaration submission window?
- [ ] Can admin extend the POI deadline after the initial window closes?
- [ ] Does the "2 employee(s) yet to submit POI" count include old regime employees only?
- [ ] Is there a bulk approve option for multiple POI submissions at once?
