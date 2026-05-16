# Item 99: Statutory Bonus Settings

**URL:** `https://payroll.zoho.in/#/settings/statutory-details/edit-statutorybonus-details`  
**Entry Points:**
- Settings > Statutory Components > Statutory Bonus tab → "Enable Statutory Bonus" link
- Direct URL when re-configuring

**Module:** Settings > Statutory Components  
**State in lerno org:** Not configured (onboarding state)

---

## Screenshots

- `screenshots/99-statutory-bonus-config.png` — Statutory Bonus configuration form (Monthly mode)

---

## Onboarding State (Not Configured)

**URL:** `#/settings/statutory-details/list/statutory-bonus`

Displayed when Statutory Bonus is not enabled:
- Heading: "Are your employees eligible to receive statutory bonus?"
- Body: "According to the Payment of Bonus Act, 1965, an eligible employee can receive a statutory bonus of 8.33% (min) to 20% (max) of their salary earned during a financial year. Configure statutory bonus of your organisation and start paying your employees."
- CTA link: "Enable Statutory Bonus" → navigates to `#/settings/statutory-details/edit-statutorybonus-details`

---

## Configuration Form — "Statutory Bonus"

### Payment Frequency — Radio Group

Two modes with different form fields:

| Option | Label | Default |
|--------|-------|---------|
| Monthly | Monthly | Selected (default) |
| Yearly | Yearly | Not selected |

---

### Monthly Mode Fields

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Monthly Percentage of Bonus | Spinbutton (%) | Yes | 8.33% to 20% | Of salary or minimum wage earned this year |

**Inline validation note (icon + text):**
> "Statutory Bonus rate should be in-between 8.33% and 20%, based on the Statutory Bonus Act."

**Notes displayed:**
- "The payment frequency of this statutory bonus is monthly and taxable."
- "Once you've associated the statutory bonus with an employee, you can change the bonus percentage only at the beginning of the next fiscal year."

**Minimum Wage section (Monthly mode):**
- State: Kerala
- Message: "Minimum wage details are not added for Kerala"
- Action: "Add Minimum Wage" (clickable div — inline trigger)
- No table shown in Monthly mode — only the empty-state message + Add link

---

### Yearly Mode Fields

Additional field compared to Monthly mode:

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Yearly Percentage of Bonus | Spinbutton (%) | Yes | 8.33% to 20% | Of salary or minimum wage earned this year |
| Payout Month | Dropdown | Yes | Any of 12 months | Month in which annual bonus is paid |

**Payout Month options:** January, February, March, April, May, June, July, August, September, October, November, December

**Notes displayed in Yearly mode:**
- "The payment frequency of this statutory bonus is yearly."
- (Same lock note about changing percentage at FY start)

**Minimum Wage Table (Yearly mode — revealed):**

| Column | Type | Notes |
|--------|------|-------|
| Employment Category | Text input | User-defined category name |
| Min Wage (per month) | Spinbutton | Minimum wage amount in ₹ |
| Effective From | Text input (date) | Format: `M yyyy` |
| Action | Button | Delete this minimum wage row |

Table starts empty. "Add Minimum Wage" appends an inline editable row.

**Statutory Bonus computation note:**
> "Statutory Bonus is a percentage of either the minimum wage or Basic + DA (whichever is higher)."

This means: `Statutory Bonus = Bonus% × MAX(Minimum Wage, Basic + DA)`

---

## Actions

| Action | Type | Behavior |
|--------|------|----------|
| Save | Button | Saves configuration and enables Statutory Bonus |
| Cancel | Link | Returns to `#/settings/statutory-details/list/statutory-bonus` |
| Add Minimum Wage | Clickable div | Appends editable row to minimum wage table (inline, no modal) |
| Delete minimum wage row | Icon button | Removes that minimum wage entry |

---

## Business Rules

1. **Payment Bonus Act, 1965** governs statutory bonus — applicable to employees earning ≤ ₹21,000/month (as of current notification).
2. **Rate range:** 8.33% (minimum) to 20% (maximum) of salary or minimum wage (whichever higher).
3. **8.33% = 1/12** — equivalent to one month's salary per year.
4. **Bonus base:** Higher of (a) minimum wage for the employment category, or (b) Basic + DA.
5. **Monthly mode:** Bonus paid each month as part of salary. This is a common practice for compliance convenience.
6. **Yearly mode:** Bonus paid once in the Payout Month. Requires minimum wage configuration per employment category.
7. **Taxable:** Statutory bonus is fully taxable as salary income.
8. **Rate lock:** Once associated with employees, bonus percentage can only be changed at the start of the next fiscal year.
9. **Minimum wage is state + employment-category specific** — Kerala has different minimum wages for skilled, semi-skilled, unskilled categories.
10. **No retroactive application** — enabling after employees are onboarded does not auto-apply to past pay runs.

---

## Statutory References

- Payment of Bonus Act, 1965
- Bonus eligibility ceiling: ₹21,000/month salary (notification 2015)
- Minimum wages: Minimum Wages Act, 1948 (state-specific notifications)
- Bonus computation: Sec 2(13) — "salary or wage" definition; Sec 11 — set-on/set-off provisions
- Taxability: Under Sec 17(1) of Income Tax Act, bonus is part of "Salary"

---

## Cross-Module Impact

- Statutory Bonus → Salary Structure: appears as a salary component (earning) on employee salary structures
- Statutory Bonus → Pay Run: deducted/added per run based on frequency setting
- Statutory Bonus → TDS: contributes to taxable income → affects TDS computation
- Statutory Bonus → Payslip: appears as separate earning line item

---

## Open Questions

- [ ] For Monthly mode, does the system compute 8.33% of each month's salary, or annualises and divides?
- [ ] What happens to employees whose salary exceeds ₹21,000 (the eligibility ceiling per Bonus Act) — does Zoho automatically exclude them?
- [ ] Is there a "set-on" / "set-off" mechanism (Bonus Act provision for carrying forward surplus/deficit bonus)?
- [ ] When Yearly mode is selected, does the system accrue the bonus month-by-month internally and pay in Payout Month?
- [ ] Can the Statutory Bonus be associated with specific employees (not all) — or is it applied org-wide once enabled?
- [ ] What is the "Employment Category" field in the minimum wage table — is this linked to an employee attribute (designation, employment type)?
- [ ] Can Zoho accommodate states where minimum wage varies by industry/sector?
