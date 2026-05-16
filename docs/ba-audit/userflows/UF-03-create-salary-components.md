# UF-03: Create Custom Earnings (HRA + Special Allowance)

**Module:** Settings → Setup & Configurations → Salary Components → Earnings
**Tested:** 2026-05-16
**Mock Data Used:** N/A (creating new components)

## Steps Executed
1. Navigated to `#/settings/salary-components/earnings`
2. Observed existing earnings list (14 pre-built components)
3. Clicked "Add Component" → dropdown appeared with 5 options
4. Selected "Earning" → navigated to `#/settings/salary-components/earnings/new`
5. Selected Earning Type: "Custom Allowance" for Special Allowance
6. Filled name "Special Allowance", set Active, unchecked EPF
7. Saved → redirected to earnings list with 15 components
8. HRA already existed as a system component — documented its configuration

## Existing Earnings Components (Pre-built by Zoho)

| Name | Earning Type | Calculation Type | EPF | ESI | Status |
|------|-------------|-----------------|-----|-----|--------|
| Basic | Basic | Fixed; 50% of CTC | Yes | Yes | Active |
| House Rent Allowance | House Rent Allowance | Fixed; 50% of Basic | No | Yes | Active |
| Conveyance Allowance | Conveyance Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Active |
| Children Education Allowance | Children Education Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Inactive |
| Transport Allowance | Transport Allowance | Fixed; Flat amount of 1600 | Yes (If PF Wage < 15k) | Yes | Inactive |
| Travelling Allowance | Travelling Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Inactive |
| Fixed Allowance | Fixed Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Active |
| Overtime Allowance | Overtime Allowance | Variable; Flat Amount | No | Yes | Inactive |
| Gratuity | Gratuity | Variable; Flat Amount | No | No | Active |
| Bonus | Bonus | Variable; Flat Amount | No | No | Active |
| Commission | Commission | Variable; Flat Amount | No | Yes | Active |
| Leave Encashment | Leave Encashment | Variable; Flat Amount | No | No | Active |
| Notice Pay | Notice Pay | Variable; Flat Amount | No | No | Active |
| Hold Salary | Hold Salary (Non Taxable) | Variable; Flat Amount | No | No | Active |

## Add Component Dropdown Options
When "Add Component" is clicked:
- Earning → `#/settings/salary-components/earnings/new`
- Correction → `#/settings/salary-components/correction/new`
- Benefit → `#/settings/salary-components/benefits/new`
- Deduction → `#/settings/salary-components/deductions/new`
- Reimbursement → `#/settings/salary-components/reimbursements/new`

## New Earning Form Fields

| Field | Type | Required | Default | Options/Rules |
|-------|------|----------|---------|---------------|
| Earning Type | Dropdown | Yes | — | See full type list below |
| Earning Name | Text | Yes | — | Free text; becomes component identifier |
| Name in Payslip | Text | Yes | — | Shown on employee payslip |
| Pay Type | Radio | Yes | Fixed Pay | "Fixed Pay (Fixed amount paid at the end of every month.)" / "Variable Pay (Variable amount paid during any payroll.)" |
| Calculation Type | Radio | Yes | Flat Amount | "Flat Amount" / "Percentage of Basic" (shown for Fixed Pay) |
| Enter Amount | Number (₹) | Conditional | 0 | Monetary field with ₹ prefix; active when Flat Amount selected |
| Mark this as Active | Checkbox | No | Unchecked | Activates the component for use |

**Other Configurations section (conditional on type):**
| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Make this earning a part of the employee's salary structure | Checkbox (disabled) | Checked | Always true for earnings |
| This is a taxable earning | Checkbox (disabled) | Checked | Pre-set based on earning type |
| Calculate on pro-rata basis | Checkbox | Checked | "Pay will be adjusted based on employee working days." |
| Consider for EPF Contribution | Checkbox | Checked | Can be unchecked |
| EPF sub-radio: Always / Only when PF Wage is less than ₹15,000 | Radio | "Only when PF Wage < ₹15,000" | Conditional on EPF checkbox |
| Consider for ESI Contribution | Checkbox | Checked | Can be unchecked |
| Show this component in payslip | Checkbox (disabled) | Checked | Always true |

**Immutability note displayed**: "Once you associate this component with an employee, you will only be able to edit the Name and Amount/Percentage. The changes you make to Amount/Percentage will apply only to new employees."

## Full List of Earning Types
Basic, House Rent Allowance, Dearness Allowance, Retaining Allowance, Conveyance Allowance, Bonus, Commission, Children Education Allowance, Hostel Expenditure Allowance, Transport Allowance, Helper Allowance, Travelling Allowance, Uniform Allowance, Daily Allowance, City Compensatory Allowance, Overtime Allowance, Telephone Allowance, Fixed Medical Allowance, Project Allowance, Food Allowance, Holiday Allowance, Entertainment Allowance, **Custom Allowance**, Food Coupon, Gift Coupon, Research Allowance, Books and Periodicals Allowance, Shift Allowance, Fuel Allowance, Driver Allowance, Leave Travel Allowance, Vehicle Maintenance Allowance, Telephone And Internet Allowance

(33 types total)

## Special Allowance Component Created
- Earning Type: Custom Allowance
- Earning Name: Special Allowance
- Name in Payslip: Special Allowance
- Pay Type: Fixed Pay
- Calculation Type: Flat Amount
- Consider for EPF: No (unchecked)
- Consider for ESI: Yes (default)
- Status: Active

## HRA (Pre-existing) Configuration
- Earning Type: House Rent Allowance
- Calculation Type: Fixed; 50% of Basic
- Consider for EPF: No
- Consider for ESI: Yes
- Status: Active

Note: HRA tax exemption rule (least of: actual HRA received, 50%/40% of basic for metro/non-metro, actual rent paid minus 10% of basic) is handled by TDS engine — not visible as a component-level setting.

## UI Patterns Noted
- Earnings list is paginated (50 per page default)
- Table columns: Name (linked), Earning Type, Calculation Type, Consider for EPF, Consider for ESI, Status, More Actions (dropdown)
- "More Actions" dropdown per row (not explored — likely Edit/Delete)
- Form shows only "Earning Type" initially; rest of fields appear after type is selected
- Tabs: Earnings, Deductions, Benefits, Reimbursements

## Screenshots
- [Earnings component list](../screenshots/UF-03-earnings-list.png)
- [New earning form with Custom Allowance selected](../screenshots/UF-03-new-earning-form.png)
