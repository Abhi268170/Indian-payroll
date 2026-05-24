# Salary Components & Salary Structures — Deep Research
**Source:** payroll.zoho.in live audit + existing audit docs (UF-03, UF-05, UF-06, UF-15, UF-86)
**Date:** 2026-05-17

---

## 1. INFORMATION ARCHITECTURE

```
Settings
├── Setup & Configurations
│   ├── Pay Schedule
│   ├── Statutory Components
│   └── Salary Components          ← component library (earning/deduction/benefit/reimbursement definitions)
└── Customisations
    └── Salary Templates           ← reusable salary structure templates
```

**Employee module (separate):**
```
Employees → Employee Profile → Salary Details tab
  → Assign salary template (or build custom structure)
  → Salary Structure (per-employee copy, not linked back to template)
```

Flow: Create Components → Build Templates → Assign to Employee

---

## 2. SALARY COMPONENTS PAGE

**URL:** `#/settings/salary-components/earnings`  
**4 sub-tabs:** Earnings | Deductions | Benefits | Reimbursements  
**"Add Component" dropdown:** Earning | Correction | Benefit | Deduction | Reimbursement

---

### 2a. EARNINGS

#### List Table Columns
| Column | Notes |
|--------|-------|
| Name | Linked to edit form |
| Earning Type | Category (Basic, HRA, Custom Allowance, etc.) |
| Calculation Type | e.g. "Fixed; 50% of CTC", "Variable; Flat Amount" |
| Consider for EPF | Yes / Yes (If PF Wage < 15k) / No |
| Consider for ESI | Yes / No |
| Status | Active / Inactive |
| More Actions | Edit / Mark as Inactive / Delete |

#### 15 Pre-built Earnings (Zoho Demo Org)
| Name | Earning Type | Calc Type | EPF | ESI | Status |
|------|-------------|-----------|-----|-----|--------|
| Basic | Basic | Fixed; 50% of CTC | Yes | Yes | Active |
| House Rent Allowance | House Rent Allowance | Fixed; 50% of Basic | No | Yes | Active |
| Conveyance Allowance | Conveyance Allowance | Fixed; Flat Amount | Yes (If < 15k) | No | Active |
| Children Education Allowance | Children Education Allowance | Fixed; Flat Amount | Yes (If < 15k) | Yes | Inactive |
| Transport Allowance | Transport Allowance | Fixed; Flat ₹1,600 | Yes (If < 15k) | Yes | Inactive |
| Travelling Allowance | Travelling Allowance | Fixed; Flat Amount | Yes (If < 15k) | No | Inactive |
| Special Allowance | Custom Allowance | Fixed; Flat Amount | No | Yes | Active |
| Fixed Allowance | Fixed Allowance | Fixed; Flat Amount | Yes (If < 15k) | Yes | Active |
| Overtime Allowance | Overtime Allowance | Variable; Flat Amount | No | Yes | Inactive |
| Gratuity | Gratuity | Variable; Flat Amount | No | No | Active |
| Bonus | Bonus | Variable; Flat Amount | No | No | Active |
| Commission | Commission | Variable; Flat Amount | No | Yes | Active |
| Leave Encashment | Leave Encashment | Variable; Flat Amount | No | No | Active |
| Notice Pay | Notice Pay | Variable; Flat Amount | No | No | Active |
| Hold Salary | Hold Salary (Non Taxable) | Variable; Flat Amount | No | No | Active |

#### New Earning Form — All Fields

**Step 1: Earning Type** (required dropdown, opens rest of form)

**Full list of 33 earning types:**
Basic, House Rent Allowance, Dearness Allowance, Retaining Allowance, Conveyance Allowance, Bonus, Commission, Children Education Allowance, Hostel Expenditure Allowance, Transport Allowance, Helper Allowance, Travelling Allowance, Uniform Allowance, Daily Allowance, City Compensatory Allowance, Overtime Allowance, Telephone Allowance, Fixed Medical Allowance, Project Allowance, Food Allowance, Holiday Allowance, Entertainment Allowance, **Custom Allowance**, Food Coupon, Gift Coupon, Research Allowance, Books and Periodicals Allowance, Shift Allowance, Fuel Allowance, Driver Allowance, Leave Travel Allowance, Vehicle Maintenance Allowance, Telephone And Internet Allowance

**Step 2: Left panel (after type selected)**

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Earning Name | Text | Yes | — | Free text identifier |
| Name in Payslip | Text | Yes | — | Shown on payslip |
| Pay Type | Radio | Yes | Fixed Pay | Fixed Pay / Variable Pay |
| Calculation Type | Radio | Yes | Flat Amount | Conditional — see below |
| Enter Amount | Number (₹) | Conditional | 0 | When Flat Amount selected |
| Enter Percentage | Number (%) | Conditional | 0.00 | When % option selected |
| Mark this as Active | Checkbox | No | Unchecked | |

**Calculation Type options per earning type:**
- **Basic**: Flat Amount OR **Percentage of CTC**
- **House Rent Allowance**: Flat Amount OR **Percentage of Basic**
- **Most others**: Flat Amount only
- **Variable Pay**: Always Flat Amount (variable means entered per pay run)

**Step 3: Right panel — Other Configurations**

| Field | Editable | Default | Applies to |
|-------|----------|---------|-----------|
| Make this earning a part of salary structure | Disabled | Checked | All (always true) |
| This is a taxable earning | Disabled | Set by type | All (pre-set) |
| Calculate on pro-rata basis | Yes | Checked | All |
| Consider for EPF Contribution | Yes | Varies by type | All |
| EPF sub-radio: Always / Only when PF Wage < ₹15,000 | Conditional | "Only when < 15k" | When EPF checked |
| Consider for ESI Contribution | Yes | Checked | All |
| Include as Flexible Benefit Plan component | Yes | Unchecked | HRA + some others |
| Show this component in payslip | Disabled | Checked | All (always true) |

**EPF Wage Logic:**
- "Yes (Always)": component ALWAYS included in PF wage regardless of total (Basic)
- "Yes (If PF Wage < ₹15k)": only included when total EPF wage < ₹15,000 statutory cap
- "No": never included in PF wage

#### Immutability Rules (Critical Business Rule)

| Field | Non-Associated | Associated with Employee |
|-------|---------------|--------------------------|
| Earning Type | **Locked** (always) | **Locked** (always) |
| Earning Name | Editable | Editable |
| Name in Payslip | Editable | Editable |
| Pay Type | Editable | **Hidden** |
| Calculation Type | Editable | **Locked** |
| Amount / Percentage | Editable | Editable (new employees only) |
| Mark as Active | Editable | Editable |
| Calculate on pro-rata | Editable | **Locked** |
| Consider for EPF | Editable | **Locked** |
| Consider for ESI | Editable | **Locked** |

**Amount/Percentage changes for associated components apply ONLY to new employees. Existing employees retain previous values.**

---

### 2b. DEDUCTIONS

#### List Table Columns
| Column | Notes |
|--------|-------|
| Name | Component name |
| Deduction Type | Category |
| Deduction Frequency | Recurring / One Time |
| Status | Active / Inactive |

#### 3 Pre-built Deductions
| Name | Type | Frequency | Status |
|------|------|-----------|--------|
| Meal Card | Other Deductions | Recurring | Active |
| Withheld Salary | Withheld Salary | One Time | Active |
| Notice Pay Deduction | Notice Pay Deduction | One Time | Active |

**Note:** Statutory deductions (EPF employee, ESI employee, PT, TDS, LWF) are system-managed — NOT here. They're calculated automatically per pay run.

#### New Deduction Form Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Name in Payslip | Text | Yes | Displayed on payslip |
| Deduction Frequency | Radio | Yes | One-time / Recurring for subsequent payrolls |
| Mark this as Active | Checkbox | No | |

**No amount here — amount entered per employee during salary assignment.**

**After association immutability:** Only Name in Payslip is editable.

---

### 2c. REIMBURSEMENTS

#### List Table Columns
| Column | Notes |
|--------|-------|
| Name | Component name |
| Reimbursement Type | Category |
| Maximum Reimbursable Amount | Monthly cap (₹) |
| Status | Active / Inactive |

#### 11 Reimbursement Types
Club Reimbursement, Entertainment Reimbursement, Gadget Reimbursement, Books and Periodicals Reimbursement, Business Development Expense Reimbursement, Helper Reimbursement, Children Education Reimbursement, Hostel Expenditure Reimbursement, Research Reimbursement, Uniform Reimbursement, Internet Reimbursement

#### New Reimbursement Form Fields
| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Reimbursement Type | Dropdown | No | 11 predefined types |
| Name in Payslip | Text | Yes | |
| Include as FBP component | Checkbox | No | FBP = Flexible Benefit Plan |
| Unclaimed handling | Radio | Yes | Carry forward (fiscal year) / Don't carry forward (monthly) |
| Enter Amount (per month) | Number (₹) | Yes | Maximum reimbursable amount |
| Mark this as Active | Checkbox | No | Default: Unchecked |

---

### 2d. BENEFITS (V2 — Skip for now)
Linked to insurance/investment plans (Benefit Plan + Investment association). Complex — not needed for V1.

### 2e. CORRECTION (V2 — Skip for now)
One-time adjustment to an existing component's amount for a specific pay run. Links to an earning type (e.g. "correct Basic for this month"). Entered during pay run, not pre-configured.

---

## 3. SALARY TEMPLATES (Salary Structures)

**URL:** `#/settings/salary-templates`  
**Location:** Settings → **Customisations** (NOT Setup & Configuration)

### Template List
| Column | Notes |
|--------|-------|
| Template Name | Linked to detail view |
| Description | Shows "−" if empty |
| Status | Active / Inactive |
| More Actions | Edit / Duplicate / Delete |

### Template Detail View (Read-only)
Shows "Salary Structure" table:
- Grouped by category (Earnings, then Deductions etc.)
- Columns: Salary Components | Monthly Amount | Annual Amount
- Sub-labels: "(50.00% of CTC)", "(50.00% of Basic Amount)"
- Total row: **Cost to Company** | monthly | annual
- Action buttons: Edit | ... (more) | Close (X)

### Template Editor (Two-Panel Layout)

**Left Panel — Component Picker**
Accordion sections (collapsed by default):
1. **EARNINGS** — active earnings not already in template
2. **REIMBURSEMENTS**
3. **FBP COMPONENTS**
4. **BENEFITS**
5. **ONE TIME EARNINGS**

Each component in picker has "+" to add to template. Once added, disappears from picker.

**Right Panel — Template Editor**

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| Template Name | Text | Yes | — | Required |
| Description | Textarea | No | — | Max 500 characters |
| Annual CTC | Number (₹) | No | 0 | Drives live calculation of all rows |

**Salary Structure Table Columns:**
| Column | Notes |
|--------|-------|
| Salary Components | Name + sub-label (formula description) |
| Calculation Type | Inline editable: percentage spinbutton + label ("% of CTC", "% of Basic") or "Fixed amount" |
| Monthly Amount | Auto-calculated from CTC + formula (editable for flat amount) |
| Annual Amount | Monthly × 12 (always auto-calculated, non-editable) |

**Fixed Allowance (Critical Invariant):**
- Always present in every template (system-added, cannot remove)
- Calculation type: **"Monthly CTC − Sum of all other components"**
- Acts as residual absorber — guarantees total = 100% of CTC
- Sub-label: "Monthly CTC - Sum of all other components"
- Monthly Amount auto-computed: `Monthly CTC − Σ(all other component monthly amounts)`
- Prevents salary from going over CTC

**Live Calculation Triggers:**
- Annual CTC field change → recalculates all rows
- Percentage spinbutton change (Basic % of CTC) → recalculates Basic monthly, HRA monthly, Fixed Allowance residual
- Monthly Amount change (flat components) → recalculates Fixed Allowance residual

**Example (Annual CTC = ₹12,00,000):**
| Component | Formula | Monthly | Annual |
|-----------|---------|---------|--------|
| Basic | 50% of CTC | ₹50,000 | ₹6,00,000 |
| HRA | 50% of Basic | ₹25,000 | ₹3,00,000 |
| Special Allowance | Flat ₹0 | ₹0 | ₹0 |
| Fixed Allowance | Residual | ₹25,000 | ₹3,00,000 |
| **Cost to Company** | | **₹1,00,000** | **₹12,00,000** |

**Template Assignment Behavior:**
- Template is COPIED to employee at assignment time
- Subsequent template edits do NOT affect existing employee assignments (non-retroactive)
- Per-employee CTC and percentages can be overridden at assignment time

**Warning Note in Editor:**
> "Note: Any changes made to existing components will be applicable only for future association."

---

## 4. EMPLOYEE SALARY STRUCTURE (for reference — Phase 2)

**URL:** Employee Profile → Salary Details tab

**Structure:**
- Annual CTC / Monthly CTC (summary card)
- Salary Structure table (read-only, same layout as template detail)
  - Basic: X% of CTC
  - HRA: Y% of Basic Amount
  - Fixed Allowance: residual (no percentage shown)
  - Cost to Company: total
- Revision History (via "Revise" button → salary revision flow)
- Perquisites section

**Key difference from template:** Per-employee percentages can differ from template defaults (e.g. 57.14% Basic instead of 50%).

---

## 5. GAP ANALYSIS — OUR CODEBASE

### Existing (partial — needs extension)
```
SalaryComponent entity:
  ✓ Name, Code, FormulaType, FixedAmount, Percentage, IsTaxable, IsSystemComponent, TenantId
  ✗ Missing: ComponentCategory (Earning/Deduction/Reimbursement)
  ✗ Missing: EarningType (which of the 33 predefined types, or null for Deduction/Reimbursement)
  ✗ Missing: NameInPayslip
  ✗ Missing: PayType (Fixed/Variable)  
  ✗ Missing: IsActive
  ✗ Missing: IsAssociatedWithEmployee (determines which fields lock)
  ✗ Missing: ConsiderForEpf (bool) + EpfInclusionRule (Always/OnlyBelowCap)
  ✗ Missing: ConsiderForEsi (bool)
  ✗ Missing: CalculateOnProRata (bool)
  ✗ Missing: DeductionFrequency (for deduction type)
  ✗ Missing: UnclaimedHandling (for reimbursement type)

ComponentFormulaType enum:
  ✓ Fixed, PercentOfBasic, PercentOfGross, PercentOfCTC
  ✗ Missing: ResidualCTC (for Fixed Allowance system component)

EmployeeSalaryStructure entity:
  ✓ EmployeeId, TenantId, AnnualCTC, EffectiveFrom, EffectiveTo
  ✗ Missing: navigation to individual components/allocations

SalaryComponentConfiguration:
  ✓ Exists, basic columns configured
```

### Missing Entities
```
SalaryStructureTemplate entity (new):
  - Id, Name, Description, IsActive, TenantId + audit

SalaryStructureTemplateComponent entity (new):
  - TemplateId → SalaryStructureTemplate
  - ComponentId → SalaryComponent
  - FormulaType override (from component but overrideable in template)
  - Percentage override (e.g. 50% Basic or 40% Basic for different templates)
  - FixedAmount override
  - SortOrder

EmployeeSalaryStructureComponent entity (new):
  - StructureId → EmployeeSalaryStructure
  - ComponentId → SalaryComponent
  - FormulaType (snapshot at assignment)
  - Percentage (snapshot at assignment)
  - FixedAmount (snapshot at assignment)
  - SortOrder
```

### Missing Application Layer
```
Commands:
  CreateSalaryComponentCommand + Handler + Validator
  UpdateSalaryComponentCommand + Handler + Validator
  DeleteSalaryComponentCommand + Handler
  CreateSalaryStructureTemplateCommand + Handler + Validator
  UpdateSalaryStructureTemplateCommand + Handler + Validator
  DeleteSalaryStructureTemplateCommand + Handler

Queries:
  ListSalaryComponentsQuery + Handler (by category filter)
  GetSalaryComponentQuery + Handler
  ListSalaryStructureTemplatesQuery + Handler
  GetSalaryStructureTemplateQuery + Handler

DTOs:
  SalaryComponentDto (list + detail)
  SalaryStructureTemplateDto (list)
  SalaryStructureTemplateDetailDto (with components array)
```

### Missing API
```
SalaryComponentsController:
  GET /api/v1/salary-components?category=Earning
  POST /api/v1/salary-components
  GET /api/v1/salary-components/{id}
  PUT /api/v1/salary-components/{id}
  DELETE /api/v1/salary-components/{id}

SalaryStructuresController:
  GET /api/v1/salary-structures (templates)
  POST /api/v1/salary-structures
  GET /api/v1/salary-structures/{id}
  PUT /api/v1/salary-structures/{id}
  DELETE /api/v1/salary-structures/{id}
```

### Missing Frontend
```
SalaryComponentsPage.tsx:
  - 4-tab layout: Earnings | Deductions | Benefits | Reimbursements
  - List table per tab with correct columns
  - "Add Component" dropdown → modal for each type
  - Edit/Delete/Deactivate per row
  
SalaryStructuresPage.tsx:
  - List table: Name | Description | Status | Actions
  - Create/Edit: two-panel builder
    - Left: component picker (accordion by category)
    - Right: template name + description + CTC field + live-calc table
  - Fixed Allowance always present as residual
```

---

## 6. V1 SCOPE RECOMMENDATION

**Build for V1:**
1. Salary Components — Earnings tab (full CRUD with all fields)
2. Salary Components — Deductions tab (simple CRUD)
3. Salary Structures (Templates) — list + two-panel builder with live CTC calc
4. Fixed Allowance as always-present residual component (system component)

**Defer to V2:**
- Benefits tab (insurance/investment plan integration)
- Reimbursements tab (FBP, carry-forward logic)
- Correction components (part of pay run flow)
- Employee salary assignment UI (Phase 2 — Employee module)

---

## 7. KEY BUSINESS INVARIANTS TO ENCODE

1. **Earning Type is immutable** — once set, cannot change. Domain exception if attempted.
2. **After employee association: Calculation Type, pro-rata, EPF/ESI flags lock.** Only Name, NameInPayslip, Amount/Percentage remain editable.
3. **Amount/Percentage change for associated component applies only to new employees** — existing employee salary snapshots unchanged.
4. **Fixed Allowance**: always present in every salary structure template. Cannot be removed. Calculated as `Monthly CTC − Σ(all other component monthly amounts)`.
5. **Salary structure is a snapshot**: when template assigned to employee, values are copied — template changes don't cascade.
6. **Active only**: only Active components can be added to new templates/employee structures. Inactive ones remain in existing structures.
7. **EPF statutory cap**: "Only when PF Wage < ₹15,000" means the component contributes to EPF wage computation only when the total EPF-eligible wage is below ₹15,000/month.
