# Item 103: Giving (Donations) Module

**URL:** `https://payroll.zoho.in/#/donations`  
**Navigation:** Left sidebar → "Giving"  
**Module:** Giving / Donations  
**Audit Date:** 2026-05-15

---

## Screenshots

- `screenshots/103-giving-campaigns-dropdown.png` — Giving module (empty state with dropdown open)

---

## Giving Module — Landing Page

**URL:** `#/donations`  
**Empty state heading:** "There are no active campaigns"  
**Empty state body:** "Create campaigns to allow your employees to contribute for a cause"

### Campaign List Tabs

| Tab | Description |
|-----|-------------|
| Active Campaigns | Default view — ongoing donation campaigns (dropdown button) |
| (other states inferred) | Likely: Completed, All — accessible via dropdown |

### Actions

| Action | Description |
|--------|-------------|
| New (toolbar) | Quick "New Campaign" button |
| New Campaign (empty state CTA) | Navigates to `#/donations/new` |

---

## New Campaign Form

**URL:** `#/donations/new`  
**Title:** "New Campaign"

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Campaign Name | Text | Yes | Name of the donation drive |
| About the Campaign | Textarea | Yes | Description/purpose |
| Exemption Type | Dropdown | Yes | See options below |
| Campaign Ends on | Date picker | Yes | End date for the campaign |
| Show in Employee Portal | Checkbox | No | Unchecked by default |

**Exemption Type Options:**
1. "Section 133 - Donation eligible for 100% exemption"
2. "Section 133 - Donation eligible for 50% exemption"
3. "None - No Exemption Applicable"

**Actions:** Save | Cancel  
**Mandatory fields indicator:** "* indicates mandatory fields"

### Business Notes (displayed on form)

> "All contributions made by your employees will be considered for exemptions based on the Exemption Type selected and will be applied in their Income Tax calculations and Form 16."

> "Based on your employees' contribution, liability will be raised, and you can pay the amount deducted using their PAN."

---

## Business Rules

1. **Donation deduction from salary:** Employee donations are deducted from salary via payroll. System creates a payroll liability.
2. **Tax exemption:** Contribution qualifies under Sec 80G / Sec 80GGA (Income Tax Act) based on exemption type. The "Section 133" reference appears to be Zoho's internal classification — statutory reference is Section 80G or 80GGA for charitable donations.
3. **Form 16 integration:** Eligible donation deductions appear in Form 16 under Chapter VI-A deductions.
4. **Employee portal visibility:** Campaign can be shown to employees on the self-service portal for them to indicate contribution amounts.
5. **PAN-based disbursement:** The donation amount collected is paid to the organisation/cause using the employee's PAN for tax reporting.
6. **Active/Completed state:** Campaigns have a defined end date; after which they become "completed".

---

## Statutory References

- Section 80G, Income Tax Act, 1961 — Deduction for donations to certain funds/institutions
- Section 80GGA — Deduction for donations for scientific research or rural development

**Note:** Zoho labels this as "Section 133" which may be an internal reference — actual statutory section for donations is 80G (partial/full based on qualifying institution).

---

## Cross-Module Impact

- Giving → Pay Run: donation amounts appear as deductions in salary computation
- Giving → TDS/Form 16: eligible deductions reduce taxable income for TDS
- Giving → Deduction Reports: "Donations Summary" report in Reports module
- Giving → Employee Portal: campaigns displayed for employee opt-in

---

## Open Questions

- [ ] Is "Section 133" a Zoho internal code or an actual IT Act section? (Sec 80G is the standard reference)
- [ ] How does an employee opt in to a campaign — via portal only, or can admin specify fixed amounts?
- [ ] Is there a per-employee contribution amount field or does the employee specify via portal?
- [ ] Can campaigns be linked to specific NGOs/charities (with 80G registration numbers)?
- [ ] Does Zoho generate Form 10BE (donation certificate) or just reflect the amount in Form 16?
