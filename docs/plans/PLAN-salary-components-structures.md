# Implementation Plan: Salary Components & Salary Structures

> Source of truth: `docs/ba-audit/userflows/RESEARCH-salary-components-structures.md`
> V1 scope: New tax regime only. No old-regime logic.

---

## Overview

Two distinct features:
1. **Salary Components** — org-level library of reusable pay building blocks (5 types)
2. **Salary Structures (Templates)** — reusable CTC templates that combine components with formula overrides; lives under Customisations nav

Employee salary assignment (per-employee snapshot from a template) is **out of scope** for this plan — blocked on Employee module.

---

## Phase 0: Domain Enums (no migration yet)

### New enums to add in `Payroll.Domain/Enums/`

**`ComponentCategory.cs`**
```csharp
public enum ComponentCategory { Earning, Deduction, Reimbursement, Benefit, Correction }
```

**`EarningType.cs`** — 33 values, locked after creation
```csharp
public enum EarningType
{
    Basic, HouseRentAllowance, ConveyanceAllowance, MedicalAllowance,
    SpecialAllowance, LeaveTravelAllowance, ChildrenEducationAllowance,
    ChildrenHostelAllowance, OverTime, OvertimeFlat, Bonus, PerformanceBonus,
    FixedBonus, CommissionOnSales, AdvanceSalary, Gratuity,
    ExGratia, NightShiftAllowance, CityCompensatoryAllowance,
    DearnesAllowance, UniformAllowance, ToolAllowance, WashingAllowance,
    MobileAllowance, InternetAllowance, FoodAllowance, BooksPeriodicals,
    HighAltitudeAllowance, BorderRemoteAllowance, FixedAllowance,
    ArrearsEarning, NotInList, Other
}
```

**`EpfInclusionRule.cs`**
```csharp
public enum EpfInclusionRule { Always, OnlyWhenPfWageBelowLimit }
```

**`DeductionFrequency.cs`**
```csharp
public enum DeductionFrequency { EveryMonth, OnceAYear }
```

**`ReimbursementType.cs`** — 11 values
```csharp
public enum ReimbursementType
{
    FuelAndMaintenance, BooksPeriodicals, FoodCoupons, GiftVouchers,
    MobileAndInternet, LeaveTravelAssistance, DriverSalary, HelperAllowance,
    UniformAllowance, ChildrenEducationAllowance, Other
}
```

**`UnclaimedReimbursementHandling.cs`**
```csharp
public enum UnclaimedReimbursementHandling { DoNotPay, PayAsTaxable }
```

**`BenefitType.cs`**
```csharp
public enum BenefitType { VPF, NPS, OtherNonTaxable }
```

**`PayType.cs`** — for earnings
```csharp
public enum PayType { Monthly, FlatAmount }
```

**Extend `ComponentFormulaType.cs`** — add `ResidualCTC` for Fixed Allowance
```csharp
public enum ComponentFormulaType { Fixed, PercentOfBasic, PercentOfGross, PercentOfCTC, ResidualCTC }
```

---

## Phase 1: Domain Entities

### 1.1 Rewrite `SalaryComponent`

Replace the current stub with a complete entity. Key design decisions:
- Single table, category discriminator (no TPH — fields are sparse enough)
- `EarningType` is nullable; only populated when `Category == Earning`
- `ForCorrectionOfComponentId` is nullable FK to self; only populated when `Category == Correction`
- Immutability enforced via domain methods, not just DB constraints

```csharp
// Payroll.Domain/Entities/SalaryComponent.cs

public sealed class SalaryComponent : AuditableEntity
{
    private SalaryComponent() { }

    // ── Identity ──────────────────────────────────────────────────────────
    public string Name { get; private set; } = string.Empty;
    public string NameInPayslip { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public ComponentCategory Category { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsSystemComponent { get; private set; }   // true = Fixed Allowance
    public bool IsActive { get; private set; } = true;

    // ── Earning-specific ──────────────────────────────────────────────────
    // Locked after creation.
    public EarningType? EarningType { get; private set; }
    // Locked after any employee is associated.
    public PayType? PayType { get; private set; }
    public ComponentFormulaType? FormulaType { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }
    public bool? IsTaxable { get; private set; }
    public bool? ConsiderForEpf { get; private set; }
    public EpfInclusionRule? EpfInclusionRule { get; private set; }
    public bool? ConsiderForEsi { get; private set; }
    public bool? CalculateOnProRata { get; private set; }
    public bool? ShowInPayslip { get; private set; }

    // ── Deduction-specific ────────────────────────────────────────────────
    public DeductionFrequency? DeductionFrequency { get; private set; }

    // ── Reimbursement-specific ────────────────────────────────────────────
    public ReimbursementType? ReimbursementType { get; private set; }
    public decimal? ReimbursementAmount { get; private set; }
    public UnclaimedReimbursementHandling? UnclaimedHandling { get; private set; }

    // ── Benefit-specific ──────────────────────────────────────────────────
    public BenefitType? BenefitType { get; private set; }
    public decimal? BenefitPercentage { get; private set; }   // for VPF (% of PF)
    public bool? IsApplicableToAllEmployees { get; private set; }
    public bool? IsNpsGovernmentSector { get; private set; }  // NPS only: 14% vs 10%

    // ── Correction-specific ───────────────────────────────────────────────
    // FK to the earning this correction adjusts. Locked after creation.
    public Guid? ForCorrectionOfComponentId { get; private set; }
    public SalaryComponent? ForCorrectionOfComponent { get; private set; }

    // ── Lock state ────────────────────────────────────────────────────────
    // Set to true when first employee salary structure references this component.
    public bool IsAssociatedWithEmployee { get; private set; }

    // ── Factory methods ───────────────────────────────────────────────────

    public static SalaryComponent CreateEarning(
        string name, string nameInPayslip, string code, EarningType earningType,
        PayType payType, ComponentFormulaType formulaType,
        decimal? fixedAmount, decimal? percentage,
        bool isTaxable, bool considerForEpf, EpfInclusionRule epfRule,
        bool considerForEsi, bool calculateOnProRata, bool showInPayslip,
        Guid tenantId, Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Earning,
            EarningType = earningType,
            PayType = payType,
            FormulaType = formulaType,
            FixedAmount = fixedAmount,
            Percentage = percentage,
            IsTaxable = isTaxable,
            ConsiderForEpf = considerForEpf,
            EpfInclusionRule = epfRule,
            ConsiderForEsi = considerForEsi,
            CalculateOnProRata = calculateOnProRata,
            ShowInPayslip = showInPayslip,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateDeduction(
        string name, string nameInPayslip, string code,
        DeductionFrequency frequency,
        Guid tenantId, Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Deduction,
            DeductionFrequency = frequency,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateReimbursement(
        string name, string nameInPayslip, string code,
        ReimbursementType reimbursementType, decimal amount,
        UnclaimedReimbursementHandling unclaimedHandling,
        Guid tenantId, Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Reimbursement,
            ReimbursementType = reimbursementType,
            ReimbursementAmount = amount,
            UnclaimedHandling = unclaimedHandling,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateBenefit(
        string name, string nameInPayslip, string code,
        BenefitType benefitType, decimal? benefitPercentage,
        bool isApplicableToAllEmployees, bool? isNpsGovernmentSector,
        Guid tenantId, Guid createdBy) => new()
        {
            Name = name,
            NameInPayslip = nameInPayslip,
            Code = code,
            Category = ComponentCategory.Benefit,
            BenefitType = benefitType,
            BenefitPercentage = benefitPercentage,
            IsApplicableToAllEmployees = isApplicableToAllEmployees,
            IsNpsGovernmentSector = isNpsGovernmentSector,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public static SalaryComponent CreateCorrection(
        string correctionName, string code,
        SalaryComponent parentEarning,
        Guid tenantId, Guid createdBy) => new()
        {
            Name = correctionName,
            NameInPayslip = correctionName,
            Code = code,
            Category = ComponentCategory.Correction,
            ForCorrectionOfComponentId = parentEarning.Id,
            // Inherit config from parent
            EarningType = parentEarning.EarningType,
            IsTaxable = parentEarning.IsTaxable,
            ConsiderForEpf = parentEarning.ConsiderForEpf,
            EpfInclusionRule = parentEarning.EpfInclusionRule,
            ConsiderForEsi = parentEarning.ConsiderForEsi,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    // ── Mutations (immutability rules enforced here) ───────────────────────

    // Always editable on any component type
    public void UpdateName(string name, string nameInPayslip)
    {
        Name = name;
        NameInPayslip = nameInPayslip;
    }

    // Editable only before employee association — for earnings
    public void UpdateEarningFormula(
        ComponentFormulaType formulaType,
        decimal? fixedAmount, decimal? percentage,
        bool isTaxable, bool considerForEpf, EpfInclusionRule epfRule,
        bool considerForEsi, bool calculateOnProRata)
    {
        if (IsAssociatedWithEmployee)
            throw new InvalidOperationException("Cannot change formula after employee association.");
        FormulaType = formulaType;
        FixedAmount = fixedAmount;
        Percentage = percentage;
        IsTaxable = isTaxable;
        ConsiderForEpf = considerForEpf;
        EpfInclusionRule = epfRule;
        ConsiderForEsi = considerForEsi;
        CalculateOnProRata = calculateOnProRata;
    }

    // Editable only after association, only amount
    public void UpdateFixedAmount(decimal amount)
    {
        if (FormulaType != ComponentFormulaType.Fixed)
            throw new InvalidOperationException("Only fixed-amount components support direct amount update.");
        FixedAmount = amount;
    }

    public void MarkAssociatedWithEmployee() => IsAssociatedWithEmployee = true;

    public void SetActive(bool active) => IsActive = active;
}
```

### 1.2 Add `SalaryStructureTemplate` entity

```csharp
// Payroll.Domain/Entities/SalaryStructureTemplate.cs

public sealed class SalaryStructureTemplate : AuditableEntity
{
    private SalaryStructureTemplate() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<SalaryStructureComponent> _components = [];
    public IReadOnlyList<SalaryStructureComponent> Components => _components.AsReadOnly();

    public static SalaryStructureTemplate Create(
        string name, string? description, Guid tenantId, Guid createdBy) => new()
        {
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedBy = createdBy,
        };

    public void UpdateName(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetComponents(IEnumerable<SalaryStructureComponent> components)
    {
        _components.Clear();
        _components.AddRange(components);
    }

    public void SetActive(bool active) => IsActive = active;
}
```

### 1.3 Add `SalaryStructureComponent` junction entity

```csharp
// Payroll.Domain/Entities/SalaryStructureComponent.cs

// Junction between SalaryStructureTemplate and SalaryComponent.
// Stores per-template formula overrides (a component can appear in many templates
// with different amounts/percentages).
public sealed class SalaryStructureComponent : AuditableEntity
{
    private SalaryStructureComponent() { }

    public Guid TemplateId { get; private set; }
    public Guid ComponentId { get; private set; }
    public SalaryComponent? Component { get; private set; }

    // Override values (null = use component defaults)
    public ComponentFormulaType FormulaType { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal? Percentage { get; private set; }

    public int DisplayOrder { get; private set; }

    public static SalaryStructureComponent Create(
        Guid templateId, Guid componentId,
        ComponentFormulaType formulaType,
        decimal? fixedAmount, decimal? percentage,
        int displayOrder) => new()
        {
            TemplateId = templateId,
            ComponentId = componentId,
            FormulaType = formulaType,
            FixedAmount = fixedAmount,
            Percentage = percentage,
            DisplayOrder = displayOrder,
        };
}
```

---

## Phase 2: Infrastructure — EF Configs & Migration

### 2.1 Update `SalaryComponentConfiguration`

Replace stub with full config:
- All new enum columns: `HasConversion<string>()`
- Nullable monetary columns: `numeric(18,4)` or `numeric(7,4)` per field
- Self-referencing FK: `ForCorrectionOfComponentId` → `SalaryComponent.Id`
- Unique index: `(TenantId, Code)` (already exists)
- Soft-delete query filter (already exists)

### 2.2 Add `SalaryStructureTemplateConfiguration`

```
- PK: Id
- Name: varchar(200), required
- Description: varchar(500), nullable
- TenantId: required, index
- Soft-delete query filter
- Nav: HasMany(Components).WithOne().HasForeignKey(c => c.TemplateId).OnDelete(Cascade)
```

### 2.3 Add `SalaryStructureComponentConfiguration`

```
- PK: Id
- TemplateId + ComponentId: composite unique index
- FormulaType: HasConversion<string>()
- FixedAmount: numeric(18,4)
- Percentage: numeric(7,4)
- DisplayOrder: int
- Nav: HasOne(Component).WithMany().HasForeignKey(c => c.ComponentId).OnDelete(Restrict)
```

### 2.4 Migration

One migration: `AddSalaryComponentsCategoriesAndStructureTemplates`
- Alters `salary_components` table (add all new columns)
- Creates `salary_structure_templates` table
- Creates `salary_structure_components` junction table

Seed system component **Fixed Allowance** in migration `Up()`:
```sql
INSERT INTO {schema}.salary_components
  (id, name, name_in_payslip, code, category, formula_type,
   is_system_component, is_active, tenant_id, created_at)
-- one row per tenant that already exists
-- OR: create it at tenant provisioning time instead (preferred)
```

**Preferred approach**: Create Fixed Allowance at tenant provisioning time (in `TenantProvisioningService`), not in a data migration. Add it to the provisioning seed alongside StatutoryConfig rows.

---

## Phase 3: Application Layer

### 3.1 Repository Interface

```csharp
// Payroll.Application/Interfaces/ISalaryComponentRepository.cs
public interface ISalaryComponentRepository
{
    Task<SalaryComponent?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<SalaryComponent>> ListByTenantAsync(Guid tenantId, ComponentCategory? category, CancellationToken ct);
    Task AddAsync(SalaryComponent component, CancellationToken ct);
    Task<bool> ExistsCodeAsync(Guid tenantId, string code, Guid? excludeId, CancellationToken ct);
    Task<bool> IsReferencedByTemplateAsync(Guid componentId, CancellationToken ct);
    // Used by Correction creation to fetch active earnings
    Task<List<SalaryComponent>> ListActiveEarningsAsync(Guid tenantId, CancellationToken ct);
}

// Payroll.Application/Interfaces/ISalaryStructureTemplateRepository.cs
public interface ISalaryStructureTemplateRepository
{
    Task<SalaryStructureTemplate?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<SalaryStructureTemplate?> GetByIdWithComponentsAsync(Guid id, CancellationToken ct);
    Task<List<SalaryStructureTemplate>> ListByTenantAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(SalaryStructureTemplate template, CancellationToken ct);
}
```

### 3.2 DTOs

```
SalaryComponentSummaryDto      — Id, Name, Code, Category, IsActive, IsSystemComponent
SalaryComponentDetailDto       — All fields (union of all category fields, nullable)
SalaryStructureTemplateSummaryDto — Id, Name, Description, IsActive, ComponentCount
SalaryStructureTemplateDetailDto  — Id, Name, Description, IsActive + nested SalaryStructureComponentDto[]
SalaryStructureComponentDto    — ComponentId, ComponentName, Category, FormulaType, FixedAmount, Percentage, DisplayOrder
```

### 3.3 Commands

**Salary Components:**

| Command | Handler notes |
|---|---|
| `CreateEarningCommand` | Validate EarningType not duplicate in tenant; generate Code if not provided |
| `CreateDeductionCommand` | |
| `CreateReimbursementCommand` | |
| `CreateBenefitCommand` | Validate BenefitType uniqueness per tenant (VPF/NPS only one allowed) |
| `CreateCorrectionCommand` | Validate parent component is active earning in same tenant |
| `UpdateSalaryComponentCommand` | Conditional update based on `IsAssociatedWithEmployee` flag; category-aware |
| `SetSalaryComponentActiveCommand` | Toggle IsActive; guard: cannot deactivate if referenced by active template |

**Salary Structures:**

| Command | Handler notes |
|---|---|
| `CreateSalaryStructureTemplateCommand` | Validate: must include Fixed Allowance (system component); validate CTC percentages don't exceed 100% |
| `UpdateSalaryStructureTemplateCommand` | Replace all SalaryStructureComponents (delete-and-reinsert); same CTC validations |
| `SetSalaryStructureTemplateActiveCommand` | Toggle; guard: cannot deactivate if assigned to employees |

### 3.4 Queries

| Query | Returns |
|---|---|
| `ListSalaryComponentsQuery` | `List<SalaryComponentSummaryDto>` — filterable by Category |
| `GetSalaryComponentQuery` | `SalaryComponentDetailDto` |
| `ListActiveEarningsForCorrectionQuery` | `List<SalaryComponentSummaryDto>` — only active earnings |
| `ListSalaryStructureTemplatesQuery` | `List<SalaryStructureTemplateSummaryDto>` |
| `GetSalaryStructureTemplateQuery` | `SalaryStructureTemplateDetailDto` |

### 3.5 Validators (FluentValidation)

**CreateEarningCommandValidator:**
- Name: required, max 100
- Code: optional, if provided: max 50, alphanumeric+underscore
- EarningType: required, must be valid enum, not `FixedAllowance` (system-only)
- FormulaType: required; if Fixed → FixedAmount required > 0; if Percent* → Percentage required 0–100
- Mutual exclusion: fixedAmount null when formula is percent, and vice versa

**CreateBenefitCommandValidator:**
- BenefitType: required
- If VPF: BenefitPercentage required (0–100% of PF wage)
- If NPS: IsNpsGovernmentSector required
- If OtherNonTaxable: Name required

**CreateCorrectionCommandValidator:**
- ForCorrectionOfComponentId: required, must resolve to active earning in same tenant

**CreateSalaryStructureTemplateCommandValidator:**
- Components: must not be empty
- Must contain exactly one component with FormulaType = ResidualCTC (Fixed Allowance)
- PercentOfCTC components: sum of percentages ≤ 100
- No duplicate ComponentIds

---

## Phase 4: API Layer

### 4.1 `SalaryComponentsController`

Route prefix: `/api/v1/salary-components`

| Method | Route | Command/Query |
|---|---|---|
| GET | `/` | `ListSalaryComponentsQuery` (query param: `?category=Earning`) |
| GET | `/{id}` | `GetSalaryComponentQuery` |
| POST | `/earnings` | `CreateEarningCommand` |
| POST | `/deductions` | `CreateDeductionCommand` |
| POST | `/reimbursements` | `CreateReimbursementCommand` |
| POST | `/benefits` | `CreateBenefitCommand` |
| POST | `/corrections` | `CreateCorrectionCommand` |
| PUT | `/{id}` | `UpdateSalaryComponentCommand` |
| PUT | `/{id}/active` | `SetSalaryComponentActiveCommand` |
| GET | `/active-earnings` | `ListActiveEarningsForCorrectionQuery` |

### 4.2 `SalaryStructureTemplatesController`

Route prefix: `/api/v1/salary-structure-templates`

| Method | Route | Command/Query |
|---|---|---|
| GET | `/` | `ListSalaryStructureTemplatesQuery` |
| GET | `/{id}` | `GetSalaryStructureTemplateQuery` |
| POST | `/` | `CreateSalaryStructureTemplateCommand` |
| PUT | `/{id}` | `UpdateSalaryStructureTemplateCommand` |
| PUT | `/{id}/active` | `SetSalaryStructureTemplateActiveCommand` |

---

## Phase 5: Frontend

### 5.1 Navigation

Salary Components lives under **Settings** sidebar group.
Salary Structures (Templates) lives under **Customisations** sidebar group.

Update `Sidebar.tsx` and `App.tsx` routes accordingly.

### 5.2 `SalaryComponentsPage.tsx`

**URL:** `/settings/salary-components`

Layout:
- Page header: "Salary Components" + "Add Component" button with dropdown (5 options)
- Tab bar: All | Earnings | Deductions | Reimbursements | Benefits | Corrections
- Table per tab: Name | Code | Type | Amount/% | Status | Actions

**Add Component** dropdown opens category-specific modal:

**`AddEarningModal.tsx`**
- Step 1: Select EarningType (grouped: Basic Pay / Allowances / Bonuses / Other)
- Step 2: Configure (name, payslip name, pay type, formula type + amount/%, EPF/ESI/ProRata toggles, taxable)
- Fields lock based on EarningType selection (e.g., HRA always taxable)

**`AddDeductionModal.tsx`**
- Name, payslip name, frequency radio (Every Month / Once a Year)

**`AddReimbursementModal.tsx`**
- ReimbursementType selector, name, payslip name, amount, unclaimed handling radio

**`AddBenefitModal.tsx`**
- BenefitType selector (VPF / NPS / Other Non-Taxable)
- Conditional fields per type:
  - VPF: percentage of PF wage
  - NPS: government/private sector radio
  - Other: name, payslip name, applicable to all employees toggle

**`AddCorrectionModal.tsx`**
- "Create Correction for" dropdown (fetches `/active-earnings`)
- Info banner: "Will have same config as [X]"
- Correction name (only editable field)
- Read-only preview of inherited EPF/ESI/taxable config

**Edit component**: inline row action → same modal pre-populated, fields disabled per immutability rules.

**Toggle active**: row action → optimistic update + `PUT /{id}/active`

### 5.3 `SalaryStructuresPage.tsx`

**URL:** `/customisations/salary-structures`

**List view:**
- Page header: "Salary Structures" + "New Structure" button
- Table: Name | Components | Status | Actions (Edit / Duplicate / Toggle)

**`SalaryStructureBuilderPage.tsx`** (full-page, not modal)

**URL:** `/customisations/salary-structures/new` and `/customisations/salary-structures/:id/edit`

Two-panel layout:
```
┌─────────────────────────┬──────────────────────────────────────┐
│  Component Picker       │  Template Editor                     │
│  (left, w-72)           │  (right, flex-1)                     │
│                         │                                      │
│  [search input]         │  Template Name ___________________   │
│                         │  Description  ___________________   │
│  ▼ Earnings (8)         │                                      │
│    Basic               →│  ┌─────────────────────────────────┐ │
│    HRA                 →│  │ Component   Formula   Amount    │ │
│    Special Allow       →│  │ Basic       % of CTC  40%       │ │
│                         │  │ HRA         % of Basic 50%       │ │
│  ▶ Deductions (2)       │  │ Fixed Allow Residual  —          │ │
│  ▶ Reimbursements (3)   │  └─────────────────────────────────┘ │
│  ▶ Benefits (1)         │                                      │
│                         │  CTC Breakdown                      │
│                         │  Annual CTC: ₹6,00,000              │
│                         │  Basic: ₹2,40,000 (40%)             │
│                         │  HRA: ₹1,20,000 (20%)               │
│                         │  Fixed Allowance: ₹2,40,000 (40%)   │
└─────────────────────────┴──────────────────────────────────────┘
```

**Interactions:**
- Click component in picker → adds to template with default formula from component
- Each row in editor: formula type dropdown (Fixed / %Basic / %Gross / %CTC) + amount/% input
- Fixed Allowance row: formula locked to Residual, amount computed and read-only
- Live CTC preview: user enters "preview CTC" amount → all amounts computed in real time (frontend only, no API call)
- Save: `POST /salary-structure-templates` or `PUT /{id}`
- Unsaved changes guard: confirm dialog on navigate away

### 5.4 API client additions (`web/src/lib/api.ts` or separate `salaryComponents.ts`)

```typescript
// Types
interface SalaryComponentSummary { id: string; name: string; code: string; category: ComponentCategory; isActive: boolean; isSystemComponent: boolean }
interface SalaryComponentDetail extends SalaryComponentSummary { /* all fields */ }
interface SalaryStructureTemplateSummary { id: string; name: string; description?: string; isActive: boolean; componentCount: number }
interface SalaryStructureTemplateDetail extends SalaryStructureTemplateSummary { components: SalaryStructureComponentDto[] }

// Query keys
['salary-components', tenantId, category?]
['salary-component', id]
['active-earnings']
['salary-structure-templates']
['salary-structure-template', id]
```

---

## Phase 6: Tests

### 6.1 Engine tests (`Payroll.Engine.Tests`)

- `FixedAllowanceCalculatorTests`: verify residual = AnnualCTC − Σ(all other components); edge cases: all components = CTC (residual = 0), oversubscribed (negative residual → validation error)
- `SalaryTemplateCtcCalculatorTests`: given template + CTC → verify component amounts
- `EpfInclusionRuleTests`: PF wage < ₹15,000 boundary conditions

### 6.2 Application tests (`Payroll.Application.Tests`)

- `CreateEarningCommandHandlerTests`: valid + duplicate code + system type rejected
- `CreateBenefitCommandHandlerTests`: VPF uniqueness, NPS uniqueness
- `CreateCorrectionCommandHandlerTests`: invalid parent (inactive, wrong tenant, non-earning)
- `UpdateSalaryComponentCommandHandlerTests`: locked fields after association
- `CreateSalaryStructureTemplateCommandHandlerTests`: missing Fixed Allowance, CTC > 100%, duplicate components
- `FluentValidation` tests for all validators

### 6.3 API integration tests (`Payroll.Api.Tests`)

- CRUD happy paths for all 5 component types
- 409 on duplicate code
- 400 on missing Fixed Allowance in template
- 403 cross-tenant access

---

## Execution Order

```
Phase 0 — Add enums                           [~1h]
Phase 1 — Rewrite domain entities             [~2h]
Phase 2 — EF configs + migration              [~1h]
Phase 3 — Application layer (CRUD)            [~4h]
Phase 4 — API controllers                     [~1h]
Phase 5.1/5.2 — SalaryComponentsPage         [~4h]
Phase 5.3/5.4 — SalaryStructuresPage builder  [~6h]
Phase 6 — Tests                               [~3h]
```

**Dependencies:**
- Phase 1 → Phase 0
- Phase 2 → Phase 1
- Phase 3 → Phase 2
- Phase 4 → Phase 3
- Phase 5 → Phase 4
- Phase 6 → any phase (write tests alongside implementation)

---

## Key Invariants (must hold in all tests and engine code)

1. `FixedAmount + Percentage` mutual exclusion per FormulaType — enforced in validator and domain
2. Fixed Allowance (`IsSystemComponent = true`) cannot be deleted or deactivated
3. `EarningType` immutable after creation — no `UpdateEarningType` method exists on entity
4. After `IsAssociatedWithEmployee = true`: FormulaType, EpfRule, ConsiderForEpf, ConsiderForEsi, CalculateOnProRata, PayType, IsTaxable all locked
5. Correction `ForCorrectionOfComponentId` immutable after creation
6. Template must contain exactly one ResidualCTC component (Fixed Allowance) — validated in command handler
7. Sum of PercentOfCTC components ≤ 100 in any template (after residual absorbs the rest)
8. BenefitType VPF: max one per tenant. BenefitType NPS: max one per tenant
