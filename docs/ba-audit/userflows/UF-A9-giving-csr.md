# UF-A9: Giving / CSR Module

**Module:** Giving (left sidebar)
**Tested:** 2026-05-16
**Route:** `#/donations`
**Approach:** Navigated to Giving module, observed empty state, clicked "New Campaign", explored the campaign creation form including Exemption Type dropdown enumeration.

---

## Findings

### 1. Module Overview

**Name in UI:** "Giving"
**Left sidebar icon:** Giving (heart icon)
**Route:** `#/donations`
**Page title:** "Zoho Payroll" (no specific title on empty state)

**Purpose:** Enables organisations to run donation/CSR campaigns. Employees can elect to donate a portion of their salary to a campaign. The donation is deducted from salary and may be eligible for income tax exemption under Section 80G (now renumbered Section 133 under Finance Act 2025).

---

### 2. Giving Module Landing Page

**Empty state heading:** "There are no active campaigns"
**View toggle:** "Active Campaigns"
**Buttons:** "New" (icon), "New Campaign" (text button — both trigger same action)

**No list when no campaigns exist.** Expected table columns (inferred from domain knowledge): Campaign Name | Organisation | Exemption Type | End Date | Total Donations | Status

---

### 3. New Campaign Form

**Route:** `#/donations/new`
**Heading:** "New Campaign"

**Fields:**

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Campaign Name | Text input | Yes | Non-empty | Name shown to employees in portal |
| About the Campaign | Textarea | Yes | Non-empty | Description of what the campaign supports |
| Exemption Type | Dropdown | Yes | Must select | Tax exemption category — see options below |
| Campaign Ends on | Month picker | Yes | Format: "M yyyy" | Month when campaign closes for donations |
| Show in Employee Portal | Checkbox | No | Default: On (checked) | Whether employees can see and donate via portal |

**Save button:** "Save"

**Exemption Type dropdown options (complete enumeration):**

| Option | Tax Section | Benefit |
|--------|-------------|---------|
| Section 133 - Donation eligible for 100% exemption | New: 133 (Old: 80G(2)(a) category) | Full donation amount exempt from taxable income |
| Section 133 - Donation eligible for 50% exemption | New: 133 (Old: 80G general) | 50% of donation amount exempt |
| None - No Exemption Applicable | — | Donation is a pure salary deduction, no tax benefit |

**Statutory Note:** Section 133 is the renumbered equivalent of old Section 80G under the Finance Act 2025 income tax re-codification. Zoho has updated to new section numbers. The 100% vs 50% distinction matches the subcategories in old 80G (PM Relief Fund = 100%, general NGOs = 50%).

---

### 4. Employee Donation Flow (Expected — Not Tested)

1. Admin creates campaign → sets exemption type and end date
2. Campaign published to employee portal (if "Show in Employee Portal" checked)
3. Employee logs in → sees active campaigns → chooses a campaign → enters donation amount (per month or one-time)
4. Donation amount deducted from monthly salary
5. Donation appears as a deduction line on payslip
6. If exemption eligible: donation amount reduces taxable income in TDS computation
7. In TDS worksheet: shows as "Donations under Section 133" deduction

---

### 5. Pay Run Integration

**"Donations" appears in pay run summary:**
From the Overall Insights tab of the May 2026 pay run:
> "Donations: ₹0.00"

This confirms donations are a tracked category in pay run analytics. When donations exist:
- They appear under "Deductions" section in pay run summary
- They reduce employee's net pay
- The amount is paid/donated via a mechanism not yet explored (possibly direct to NGO, or admin collects)

---

### 6. Payslip Impact

Expected payslip line:
- Under Deductions: "Giving — [Campaign Name]: ₹X"

Tax treatment in TDS computation:
- Section 133 (100% or 50%) donations reduce Chapter VIA / Chapter VI-B equivalent deductions
- Only applicable if employee opts into old regime (with exemptions)
- Under new regime: donations still deducted from salary but do NOT reduce taxable income

---

### 7. Navigation Paths

| Entry | Route |
|-------|-------|
| Left sidebar "Giving" | `#/donations` |
| New Campaign button | `#/donations/new` |
| Campaign detail (expected) | `#/donations/{id}` |

---

## Screenshots / Files

- `giving-new-campaign.png` — New Campaign form with Exemption Type dropdown showing all options

---

## Gaps / Open Questions

- [ ] **Donation payment mechanism:** Where does the money actually go? Does Zoho integrate with NGO payment gateways, or does the admin manually transfer collected donations?
- [ ] **Employee donation portal UI:** What does the employee see when selecting a campaign? Fixed amount, recurring, or percentage of salary?
- [ ] **TDS impact under new regime:** Since v1 is new regime only, donations do NOT reduce tax. Does the system still show Section 133 exemption in TDS computation? If so, it's a UI error. 🔴
- [ ] **"Show in Employee Portal" = Off behaviour:** If unchecked, is this an admin-only campaign (admin enrolls employees directly)?
- [ ] **Campaign end date validation:** Can a campaign be ended early by admin after launch?
- [ ] **100% vs 50% dropdown categories:** Which Indian organisations qualify for 100%? Is Zoho maintaining a list, or does admin self-certify?
- [ ] **Section 133 vs 80G:** Does Zoho's Form 16 Part B use Section 133 or 80G? TRACES may use old numbering on Form 16. 🟡
