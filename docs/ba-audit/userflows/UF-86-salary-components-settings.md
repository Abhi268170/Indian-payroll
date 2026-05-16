# UF-86: Settings — Salary Components

**Module:** Settings > Setup & Configurations > Salary Components
**Tested:** 2026-05-16
**Mock Data Used:** Demo org salary components as configured
**App State Before:** `#/settings/salary-components/earnings`

## Steps Executed
1. Navigate to `#/settings/salary-components/earnings`
2. Observe full earnings components list with all attributes
3. Documented all 15 earning components

---

## Salary Components Page Layout

### URL
`#/settings/salary-components/earnings`

### Navigation (Sub-tabs)
| Tab | URL |
|-----|-----|
| Earnings | `#/settings/salary-components/earnings` |
| Deductions | `#/settings/salary-components/deductions` |
| Benefits | `#/settings/salary-components/benefits` |
| Reimbursements | `#/settings/salary-components/reimbursements` |

### Actions
- "Add Component" button — create new salary component
- "Instant Helper" button

---

## Earnings Components — Complete List

### Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name (clickable link to detail) |
| Earning Type | Category of earning (Basic, HRA, Conveyance, etc.) |
| Calculation Type | How the amount is computed |
| Consider for EPF | Whether this component is included in PF wage calculation |
| Consider for ESI | Whether this component is included in ESI wage calculation |
| Status | Active / Inactive |
| More Actions | Dropdown with Edit, Delete, Deactivate options |

### All Earnings Components

| Name | Earning Type | Calculation Type | EPF | ESI | Status |
|------|-------------|-----------------|-----|-----|--------|
| Basic | Basic | Fixed; 50% of CTC | Yes | Yes | Active |
| House Rent Allowance | House Rent Allowance | Fixed; 50% of Basic | No | Yes | Active |
| Conveyance Allowance | Conveyance Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Active |
| Children Education Allowance | Children Education Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Inactive |
| Transport Allowance | Transport Allowance | Fixed; Flat amount of 1600 | Yes (If PF Wage < 15k) | Yes | Inactive |
| Travelling Allowance | Travelling Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | No | Inactive |
| Special Allowance | Custom Allowance | Fixed; Flat Amount | No | Yes | Active |
| Fixed Allowance | Fixed Allowance | Fixed; Flat Amount | Yes (If PF Wage < 15k) | Yes | Active |
| Overtime Allowance | Overtime Allowance | Variable; Flat Amount | No | Yes | Inactive |
| Gratuity | Gratuity | Variable; Flat Amount | No | No | Active |
| Bonus | Bonus | Variable; Flat Amount | No | No | Active |
| Commission | Commission | Variable; Flat Amount | No | Yes | Active |
| Leave Encashment | Leave Encashment | Variable; Flat Amount | No | No | Active |
| Notice Pay | Notice Pay | Variable; Flat Amount | No | No | Active |
| Hold Salary | Hold Salary (Non Taxable) | Variable; Flat Amount | No | No | Active |

---

## Key Business Rules from Component Configuration

### EPF Wage Computation Logic
The "Yes (If PF Wage < 15k)" rule is significant:
- If total EPF wage is below ₹15,000, these components ARE included in PF wage
- If total EPF wage is already ≥ ₹15,000, these components are NOT additionally counted
- This reflects the statutory EPF wage cap: PF is computed on maximum ₹15,000 per month
- Components marked simply "Yes" (Basic) are always included in EPF wage regardless of limit

### Fixed vs Variable Components
| Type | When Used |
|------|-----------|
| Fixed; 50% of CTC | Computed as a percentage of CTC — automatically adjusts when CTC changes |
| Fixed; 50% of Basic | Computed as percentage of Basic salary |
| Fixed; Flat Amount | Admin enters a fixed ₹ amount per month |
| Fixed; Flat amount of 1600 | System-default Transport Allowance — ₹1,600/month (old Income Tax exemption amount) |
| Variable; Flat Amount | Entered per pay run — does not auto-compute; admin fills in each month |

### Component Status
- **Active**: Available for use in salary structures
- **Inactive**: Configured but disabled — cannot be added to new salary structures; existing assignments may continue

### Taxability
| Component | Tax Treatment |
|-----------|--------------|
| Basic | Fully taxable |
| House Rent Allowance | New regime: fully taxable; Old regime: exempt up to Section 10(13A) formula |
| Conveyance | New regime: taxable; Old regime: partially exempt |
| Transport Allowance | ₹1,600/month exempt in old regime under old Sec 10(14) — eliminated in new regime |
| Children Education Allowance | ₹100/child/month (2 children) exempt in old regime |
| Special Allowance | Fully taxable |
| Fixed Allowance | Fully taxable |
| Overtime | Fully taxable |
| Gratuity | Exempt for govt employees; up to ₹20L exempt for private employees under Gratuity Act |
| Bonus | Fully taxable |
| Commission | Fully taxable |
| Leave Encashment | Exempt on retirement (Section 10(10AA)); taxable if encashed during service |
| Notice Pay | Taxable (received) or deductible (paid by employee) |
| Hold Salary | Labeled "Non Taxable" — treated as a salary advance/hold, not income |

### HRA Configuration
- HRA = "Fixed; 50% of Basic" — standard HRA formula
- HRA is NOT included in EPF wage (correct — HRA is not part of basic wage under EPF Act)
- HRA is included in ESI wage (correct — ESI covers gross wages)

---

## Salary Structure vs Salary Component Relationship
- This settings page defines the component library (available building blocks)
- Salary Structures (Settings > Salary Templates or individual employee assignments) use subsets of these components
- Employees' actual assignments reference specific components from this library
- Inactive components cannot be added to new structures

---

---

## Deductions Components (from `#/settings/salary-components/deductions`)

### Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Deduction Type | Category |
| Deduction Frequency | Recurring (every month) or One Time |
| Status | Active / Inactive |

### All Deduction Components

| Name | Deduction Type | Frequency | Status |
|------|---------------|-----------|--------|
| Meal Card | Other Deductions | Recurring | Active |
| Withheld Salary | Withheld Salary | One Time | Active |
| Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

**Note:** Only 3 custom deduction components configured. Statutory deductions (EPF, ESI, PT, TDS, LWF) are system-managed — they do not appear here; they are computed automatically and applied to each pay run.

---

## Reimbursements Components (from `#/settings/salary-components/reimbursements`)

**Page note:** "With these reimbursement components, employees can claim reimbursements for the components which are part of the payroll and not for other expenses reimbursements."

### Table Columns
| Column | Description |
|--------|-------------|
| Name | Component name |
| Reimbursement Type | Category |
| Maximum Reimbursable Amount | Monthly cap in ₹ |
| Status | Active / Inactive |

### All Reimbursement Components

| Name | Reimbursement Type | Max Amount | Status |
|------|--------------------|-----------|--------|
| Fuel Reimbursement | Fuel Reimbursement | ₹0 | Inactive |
| Driver Reimbursement | Driver Reimbursement | ₹0 | Inactive |
| Vehicle Maintenance Reimbursement | Vehicle Maintenance Reimbursement | ₹0 | Inactive |
| Telephone Reimbursement | Telephone Reimbursement | ₹0 | Inactive |
| Leave Travel Allowance | Leave Travel Allowance | ₹0 | Inactive |

**All 5 reimbursement components are Inactive with Maximum Amount = ₹0.** This explains why there are no active reimbursement claims and the FBP settings show "No Active FBP component."

---

## Gaps / Observations
- Deductions, Benefits, and Reimbursements sub-tabs not navigated
- 🟡 "Hold Salary (Non Taxable)" — unusual label; semantics unclear. Is this "salary put on hold" (future payment) or an advance against which future salary is offset?
- Transport Allowance has a hardcoded "flat amount of 1600" — this was the pre-2018 transport exemption; new regime has no such exemption. The static ₹1,600 amount may be a legacy configuration
- Conveyance Allowance and Transport Allowance are both present — possible duplication/confusion between the two
- No NPS component visible in earnings — NPS employer contribution is a significant benefit in Indian payroll (taxable in employee hands but deductible for employer, and employee gets 80CCD(2) benefit)

## Open Questions
- [ ] What components are in the Deductions, Benefits, and Reimbursements sub-tabs?
- [ ] Can admin create entirely new component types (e.g., "Meal Allowance", "Medical Reimbursement")?
- [ ] Is there a "Flexi Benefits" or FBP component — the FBP settings showed no active FBP component?
- [ ] NPS component: Is employer NPS contribution handled as a separate component?
- [ ] What happens if a component is deactivated but it's used in an employee's current salary structure?
