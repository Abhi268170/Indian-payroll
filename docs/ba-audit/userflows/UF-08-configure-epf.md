# UF-08: Configure EPF

**Module:** Settings → Setup & Configurations → Statutory Components → EPF
**Tested:** 2026-05-16
**Mock Data Used:** EPF Number KA/KAR/1234567/001

## Steps Executed
1. Navigated to `#/settings/statutory-details/list`
2. Observed empty state: "Are you registered for EPF?" with "Enable EPF" link
3. Clicked "Enable EPF" → navigated to `#/settings/statutory-details/edit-epf-details`
4. Documented all fields on the EPF configuration form
5. Explored Employer Contribution Rate dropdown options
6. Filled EPF Number: KA/KAR/1234567/001
7. Clicked "Enable" → success toast: "EPF details updated successfully."
8. Verified saved state with all configured values displayed

## Fields & Validations

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| EPF Number | Text | No | — | Format: AA/AAA/0000000/XXX (shown as placeholder) |
| Deduction Cycle | Text (disabled) | — | Monthly | Read-only; always Monthly |
| Employee Contribution Rate | Dropdown (disabled) | — | 12% of Actual PF Wage | Fixed; not editable |
| Employer Contribution Rate | Dropdown | Yes | 12% of Actual PF Wage | Options: "12% of Actual PF Wage", "Restrict Contribution to ₹15,000 of PF Wage" |
| Include employer's contribution in employee's salary structure | Checkbox | No | Checked | Controls whether ER-PF appears in salary structure |
| Include employer's EDLI contribution in employee's salary structure | Checkbox | No | Unchecked | Has tooltip icon |
| Include admin charges in employee's salary structure | Checkbox | No | Unchecked | Has tooltip icon |
| Override PF contribution rate at employee level | Checkbox | No | Unchecked | Allows per-employee rate override |
| Pro-rate Restricted PF Wage | Checkbox (LOP config) | No | Unchecked | "PF contribution will be pro-rated based on the number of days worked by the employee." |
| Consider all applicable salary components if PF wage is less than ₹15,000 after Loss of Pay | Checkbox (LOP config) | No | Checked | "PF wage will be computed using the salary earned in that particular month (based on LOP) rather than the actual amount mentioned in the salary structure." |

## Statutory Calculations Observed (Sample Panel)

The form displays a live sample EPF Calculation panel on the right side with PF Wage = ₹20,000:

**Employee's Contribution:**
| Component | Calculation | Amount |
|-----------|-------------|--------|
| EPF | 12% of 20,000 | ₹2,400 |

**Employer's Contribution:**
| Component | Calculation | Amount |
|-----------|-------------|--------|
| EPS | 8.33% of 20,000 (Max of ₹15,000) | ₹1,250 |
| EPF | 12% of 20,000 − EPS | ₹1,150 |
| **Total** | | **₹2,400** |

Key statutory rules encoded:
- EPS is capped at ₹15,000 wage ceiling (8.33% × 15,000 = ₹1,250 max)
- Employer EPF = Total ER contribution (12%) − EPS
- Employee EPF = 12% of actual PF wage (no ceiling restriction unless ER rate is changed)

## Saved State Fields (Read View)

After enabling, the summary view shows:
- EPF Number
- Deduction Cycle: Monthly
- Employee Contribution Rate: 12% of Actual PF Wage
- Employer Contribution Rate: 12% of Actual PF Wage (View Splitup)
- Contribution Preferences (Included in Salary Structure): Employer's PF contribution (checked), EDLI contribution (unchecked), Admin charges (unchecked)
- Allow Employee level Override: No
- Pro-rate Restricted PF Wage: No
- Consider applicable salary components based on LOP: Yes (when PF wage is less than ₹15,000)
- Eligible for ABRY Scheme: No

Additional field visible in read-only view but NOT in edit form: **Eligible for ABRY Scheme** (Atmanirbhar Bharat Rojgar Yojana)

## Actions

| Action | Trigger | Pre-condition | Post-behavior |
|--------|---------|---------------|---------------|
| Enable EPF | Button "Enable" | EPF not yet enabled | Saves config, redirects to EPF list page, shows toast "EPF details updated successfully." |
| Disable EPF | Button "Disable EPF" | EPF already enabled | Presumably disables EPF across org |
| Preview EPF Calculation | Button | Any time | Opens interactive preview (not fully explored) |
| Edit | Link with pencil icon | EPF enabled | Navigates to edit form |
| Cancel | Link | Any time | Navigates to `#/settings/statutory-details/list` |

## Navigation Tabs (Statutory Components section)
- EPF (current)
- ESI
- Professional Tax
- Labour Welfare Fund
- Statutory Bonus

## Cross-Module Effects
- Enabling EPF does not auto-enable EPF at employee level — each employee must be individually opted in via `Overview → Statutory Information → Enable EPF`
- Employer PF contribution appears as line item in salary structure if "Include employer's contribution" checkbox is checked

## Gaps / Observations
- Employee contribution rate is fixed (disabled dropdown) — no option to set it to a different rate
- No field to enter EPF Trust registration details (for exempt trusts)
- ABRY Scheme field visible in read view but not configurable in edit form
- "View Splitup" button next to Employer Contribution Rate shows the EPS/EPF breakdown — appears as a tooltip/popover (not fully captured)

## Screenshots
- [EPF not enabled empty state](../screenshots/UF-08-EPF-not-enabled.png)
- [EPF configuration form](../screenshots/UF-08-EPF-form.png)
- [EPF enabled saved view](../screenshots/UF-08-EPF-enabled.png)
