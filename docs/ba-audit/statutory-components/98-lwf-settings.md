# Item 98: Labour Welfare Fund (LWF) Settings

**URL:** `https://payroll.zoho.in/#/settings/statutory-details/list/lwf`  
**Module:** Settings > Statutory Components  
**State in lerno org:** Pre-populated (Kerala ₹50/₹50 Monthly), Status = Disabled

---

## Screenshots

- `screenshots/98-lwf-settings.png` — LWF settings page

---

## LWF Settings View

**URL:** `#/settings/statutory-details/list/lwf`

Page heading: "Labour Welfare Fund"  
Descriptive text: "Labour Welfare Fund act ensures social security and improves working conditions for employees."

### Location Card: Kerala

| Field | Value | Editable |
|-------|-------|----------|
| State | Kerala | No |
| Employees' Contribution | ₹50.00 | No (system-supplied) |
| Employer's Contribution | ₹50.00 | No (system-supplied) |
| Deduction Cycle | Monthly | No (state-specific) |
| Status | Disabled | Via "(Enable)" button |

**Key observation:** LWF amounts are **pre-populated by the system** based on the state (Kerala) and are **not user-editable**. Unlike PT slabs which can be revised, LWF contribution amounts are maintained by Zoho and updated when state LWF rates change.

**"(Enable)" button:** Enables LWF deductions for this organisation. Changes status from "Disabled" to "Enabled". Likely a simple toggle — no additional configuration form.

---

## Business Rules

1. **LWF is state-specific:** Rates, contribution amounts, and deduction cycles vary by state. Kerala = ₹50 employee + ₹50 employer, Monthly.
2. **LWF amounts not user-configurable:** System maintains rate tables by state. Admin can only Enable/Disable.
3. **LWF is location-scoped:** Similar to PT, each work location applies LWF based on its state.
4. **Disabled by default:** New organisations start with LWF disabled even if state has LWF provisions.
5. **Deduction from both employee and employer:** Unlike PT (employee only), LWF has both employee and employer contributions.
6. **Monthly deduction cycle for Kerala:** ₹50 deducted from employee each month, ₹50 contributed by employer.
7. **Annual Kerala LWF:** ₹50 × 12 = ₹600/year employee + ₹600/year employer = ₹1,200/year total.

---

## LWF Rate Comparison (Reference)

Different states have different LWF rates, deduction cycles, and covered employment categories:

| State | Employee | Employer | Cycle | Notes |
|-------|----------|----------|-------|-------|
| Kerala | ₹50 | ₹50 | Monthly | |
| Maharashtra | ₹12 (Jun) / ₹24 (Dec) | ₹24 (Jun) / ₹48 (Dec) | Half Yearly | Jun & Dec |
| Karnataka | ₹10 | ₹30 | Annual | |
| Tamil Nadu | ₹10 | ₹20 | Annual | |
| Telangana | ₹2.50 | ₹5 | Monthly | |

*Reference data — not all verified in Zoho UI.*

---

## Statutory References

- Kerala Labour Welfare Fund Act, 1975
- Kerala Labour Welfare Fund (Amendment) Rules
- LWF is NOT deductible under Income Tax Act (unlike PT which is deductible under Sec 16(iii))

---

## Cross-Module Impact

- LWF deduction appears in employee payslip under "Deductions" section
- LWF employer contribution appears in payslip under "Employer Contributions" (if included in CTC)
- LWF challan generation available in Compliance module
- LWF state assignment comes from employee's work location

---

## Open Questions

- [ ] Does enabling LWF immediately affect the next pay run, or from a configurable effective date?
- [ ] Is there a "Disable" option once LWF is enabled (reverse toggle)?
- [ ] How does Zoho handle LWF rate changes by the state government — does the system auto-update or require manual trigger?
- [ ] Are LWF amounts ever different for different employment categories within the same state?
- [ ] Is employer's LWF contribution included in CTC? Is there a checkbox for this (similar to EPF/ESI)?
