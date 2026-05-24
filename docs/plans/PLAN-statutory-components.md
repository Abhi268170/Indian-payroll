# PLAN: Statutory Components Module

**Date:** 2026-05-17  
**Scope:** Full implementation of EPF, ESI, PT, LWF, TDS (new regime), Statutory Bonus, Gratuity, NPS  
**V1 constraint:** New tax regime only. Old regime: DEFERRED.

---

## Research Summary

### What we learned (non-obvious facts for engine design)

| Domain | Critical finding |
|--------|-----------------|
| EPF | PF wage = Basic + DA only. HRA/OT/bonus excluded. New Labour Code 50% rule: excluded components cannot collectively exceed 50% CTC — excess forced into PF wage. |
| EPF | EDLI Admin (Account 22) fully waived since Apr 2017. Do NOT compute or remit. |
| EPF | EPS capped at ₹15,000 wage. Employer EPF = 12% employer − EPS contribution (employer bears both, split across accounts). |
| ESI | Two separate wage values needed: `esicEligibilityWage` (excl. OT, for ₹21,000 threshold check) and `esicContributionWage` (incl. OT, for 0.75%/3.25% calc). |
| ESI | Employee crossing ₹21,000 mid-contribution-period continues until period end. No mid-period exit. |
| ESI | `isEsicNotifiedArea` flag per establishment — 89 of 691 districts not notified. |
| PT | Work-location-driven, not HQ. Maharashtra requires `employee.Gender`. Karnataka revised Apr 2025 (₹24,999 threshold). February surcharge only in Maharashtra + Karnataka. |
| PT | Half-yearly states (Kerala, TN, Puducherry): accumulate gross for the period, then apply slab. Annual states (Bihar, Jharkhand, Manipur, Meghalaya, Odisha): compute annual PT, divide by 12 monthly. |
| LWF | 15/16 states use fixed flat amounts, not % of salary. Haryana is the only %-based exception. |
| TDS | FY2025-26 slabs changed materially from FY2024-25 post-Budget 2025. Standard deduction ₹75,000. 87A rebate ₹60,000 (income ≤ ₹12L → effective zero tax up to ₹12.75L salary). |
| TDS | Recompute from scratch every month using annual projection — never carry state, always reconcile. `monthlyTds = CEILING(remainingObligation / monthsRemaining)`. |
| TDS | Employer EPF+NPS+Superannuation > ₹7.5L/year aggregate = taxable perquisite. Track this. |
| NPS | 80CCD(2) limit unified to 14% of Basic+DA for all employees from FY2025-26. |
| Gratuity | New Labour Code (Nov 2025): wage for gratuity must be ≥ 50% of CTC. Monthly CTC accrual = 4.81% of `max(Basic+DA, 0.5×CTC)`. |
| Stat Bonus | Never monthly. Lump sum within 8 months of FY close. Compute on `max(₹7,000, state_min_wage) × 12`. |

---

## Architecture: Where Each Piece Lives

```
Payroll.Domain/
  Entities/
    StatutoryOrgConfig.cs          # EPF/ESI enabled, registration numbers, settings
    EmployeeStatutoryProfile.cs    # Per-employee: UAN, PRAN, work state, VPF, PF wage option
  Interfaces/
    IStatutoryConfigRepository.cs
    IEmployeeStatutoryProfileRepository.cs
    IProfessionalTaxSlabRepository.cs
    ILwfRateRepository.cs
    IIncomeTaxConfigRepository.cs

Payroll.Engine/
  Statutory/
    EpfCalculator.cs               # Pure, no I/O
    EsiCalculator.cs               # Pure
    ProfessionalTaxCalculator.cs   # Pure
    LwfCalculator.cs               # Pure
    TdsCalculator.cs               # Pure (most complex)
    StatutoryBonusCalculator.cs    # Pure
    GratuityCalculator.cs          # Pure
  Inputs/
    EpfInputs.cs
    EsiInputs.cs
    ProfessionalTaxInputs.cs
    LwfInputs.cs
    TdsInputs.cs
  Outputs/
    EpfResult.cs
    EsiResult.cs
    ProfessionalTaxResult.cs
    LwfResult.cs
    TdsResult.cs

Payroll.Infrastructure/
  Persistence/
    Repositories/
      StatutoryConfigRepository.cs
      ProfessionalTaxSlabRepository.cs
      LwfRateRepository.cs
      IncomeTaxConfigRepository.cs
    EntityConfigurations/
      StatutoryOrgConfigConfiguration.cs
      ProfessionalTaxSlabConfiguration.cs
      LwfStateConfigConfiguration.cs
      IncomeTaxSlabConfiguration.cs
    Migrations/
      AddStatutoryTables.cs
  SeedData/
    StatutorySlabSeeder.cs         # Seeds PT slabs, LWF rates, IT slabs at tenant provision

Payroll.Application/
  Commands/Statutory/
    ConfigureEpfCommand + Handler
    ConfigureEsiCommand + Handler
    ConfigureProfessionalTaxCommand + Handler
    ConfigureLwfCommand + Handler
    SubmitItDeclarationCommand + Handler  # Employee form 12BB
  Queries/Statutory/
    GetStatutoryConfigQuery
    GetEmployeeStatutoryProfileQuery
  DTOs/
    StatutoryConfigDto.cs
    ItDeclarationDto.cs

Payroll.Api/
  Controllers/
    StatutoryComponentsController.cs

web/
  pages/settings/
    StatutoryComponentsPage.tsx     # Tab shell (EPF | ESI | Professional Tax | LWF | Statutory Bonus)
    TaxesPage.tsx                   # Separate Settings > Taxes page (TAN, PAN, AO Code, regime)
    WorkLocationsPage.tsx           # Settings > Work Locations (name, address, city, state, pin)
    statutory/
      EpfTab.tsx
      EsiTab.tsx
      PtTab.tsx
      LwfTab.tsx
      StatutoryBonusTab.tsx
    taxes/
      TaxDeductorForm.tsx
      ItDeclarationSettings.tsx
```

---

## Phase 1: DB Schema

### Migration: `AddStatutoryTables`

#### `statutory_org_config` (one row per tenant)

```sql
id                          uuid PK
tenant_id                   uuid NOT NULL UNIQUE

-- EPF
epf_enabled                         bool NOT NULL DEFAULT false
epf_establishment_code              varchar(20)              -- format AA/AAA/0000000/XXX
epf_employee_contribution_rate      varchar(30) NOT NULL DEFAULT 'ActualPfWage12'  -- dropdown value
epf_employer_contribution_rate      varchar(30) NOT NULL DEFAULT 'ActualPfWage12'  -- dropdown value
epf_include_employer_in_ctc         bool NOT NULL DEFAULT true
epf_include_edli_in_ctc             bool NOT NULL DEFAULT false
epf_include_admin_in_ctc            bool NOT NULL DEFAULT false
epf_override_at_employee_level      bool NOT NULL DEFAULT false
epf_pro_rate_restricted_pf_wage     bool NOT NULL DEFAULT false   -- "Pro-rate Restricted PF Wage"
epf_consider_salary_on_lop          bool NOT NULL DEFAULT true    -- "Consider all applicable salary components if PF wage < ₹15k after LOP"

-- ESI
esi_enabled                 bool NOT NULL DEFAULT false
esi_establishment_code      varchar(20)
esi_notified_area           bool NOT NULL DEFAULT true  -- false = no ESI regardless

-- PT
pt_enabled                  bool NOT NULL DEFAULT false

-- LWF
lwf_enabled                 bool NOT NULL DEFAULT false

-- NPS (employer)
nps_employer_enabled        bool NOT NULL DEFAULT false
nps_employer_rate           decimal(5,4)  -- e.g. 0.10 for 10%, up to 0.14

-- Audit
created_at, created_by, updated_at, updated_by
```

#### `professional_tax_slabs` (seeded at provision, admin-editable)

```sql
id              uuid PK
state_code      varchar(10) NOT NULL      -- 'MH', 'KA', 'TN', etc.
effective_date  date NOT NULL
frequency       varchar(20) NOT NULL      -- 'Monthly' | 'HalfYearly' | 'Annual'
gender          varchar(10)               -- NULL = no split; 'Male'/'Female' for Maharashtra
min_gross       decimal(18,4) NOT NULL
max_gross       decimal(18,4)             -- NULL = unbounded upper
pt_amount       decimal(18,4) NOT NULL
is_february_surcharge bool NOT NULL DEFAULT false  -- true only for MH/KA top bracket
is_active       bool NOT NULL DEFAULT true

UNIQUE(state_code, effective_date, frequency, gender, min_gross)
INDEX(state_code, effective_date)
```

#### `lwf_state_config` (seeded at provision, admin-editable)

```sql
id                  uuid PK
state_code          varchar(10) NOT NULL
effective_date      date NOT NULL
employee_amount     decimal(18,4) NOT NULL  -- fixed amount or 0 if %-based
employer_amount     decimal(18,4) NOT NULL
is_percentage_based bool NOT NULL DEFAULT false  -- true only for Haryana
employee_rate       decimal(7,4)            -- only for Haryana (0.002)
employer_rate       decimal(7,4)
rate_cap_employee   decimal(18,4)           -- monthly cap for %-based
rate_cap_employer   decimal(18,4)
frequency           varchar(20) NOT NULL    -- 'Monthly' | 'HalfYearly' | 'Annual'
deduction_month     int                     -- for Annual: month of year (Dec=12)
deposit_due_day     int                     -- day of month deposit is due
wage_threshold      decimal(18,4)           -- exempt if monthly wage > this (NULL = no threshold)
is_active           bool NOT NULL DEFAULT true

UNIQUE(state_code, effective_date)
INDEX(state_code, effective_date)
```

#### `income_tax_slabs` (seeded, FY-versioned)

```sql
id              uuid PK
fiscal_year     varchar(10) NOT NULL    -- '2025-26'
regime          varchar(20) NOT NULL    -- 'New' | 'Old' (Old: DEFERRED)
bracket_min     decimal(18,4) NOT NULL
bracket_max     decimal(18,4)           -- NULL = unbounded
rate            decimal(7,4) NOT NULL   -- e.g. 0.05 for 5%

INDEX(fiscal_year, regime, bracket_min)
```

#### `income_tax_surcharge_slabs` (seeded)

```sql
id              uuid PK
fiscal_year     varchar(10) NOT NULL
regime          varchar(20) NOT NULL
income_from     decimal(18,4) NOT NULL
income_to       decimal(18,4)
surcharge_rate  decimal(7,4) NOT NULL
```

#### `income_tax_config` (seeded, one row per FY per regime)

```sql
id                      uuid PK
fiscal_year             varchar(10) NOT NULL
regime                  varchar(20) NOT NULL
standard_deduction      decimal(18,4) NOT NULL   -- ₹75,000 for FY2025-26
rebate_87a_limit        decimal(18,4) NOT NULL   -- ₹12,00,000
rebate_87a_amount       decimal(18,4) NOT NULL   -- ₹60,000
employer_statutory_cap  decimal(18,4) NOT NULL   -- ₹7,50,000 (EPF+NPS+Super aggregate)
nps_employer_max_rate   decimal(7,4) NOT NULL    -- 0.14 for FY2025-26

UNIQUE(fiscal_year, regime)
```

#### `work_locations` table (per tenant, org-level settings)

```sql
id                  uuid PK
tenant_id           uuid NOT NULL
name                varchar(100) NOT NULL
address_line1       varchar(200)
address_line2       varchar(200)
state_code          varchar(10) NOT NULL   -- drives PT slab + LWF state for assigned employees
city                varchar(100)
pin_code            varchar(10)
is_filing_address   bool NOT NULL DEFAULT false  -- "FILING ADDRESS" badge in Zoho; only one per tenant
is_active           bool NOT NULL DEFAULT true
created_at, created_by, updated_at, updated_by

UNIQUE(tenant_id, is_filing_address) WHERE is_filing_address = true  -- only one filing address
INDEX(tenant_id, state_code)
```

**Key design:** Employees are assigned a `work_location_id`. PT and LWF state derive from the work location's `state_code` — not from a separate employee field. This matches Zoho Payroll's exact data model and legal requirement (PT/LWF are workplace-jurisdiction, not residence-jurisdiction).

#### Employee-level statutory additions (on `employees` table or separate `employee_statutory_profiles`)

```sql
-- Add to employees table OR create separate table:
work_location_id        uuid FK work_locations -- drives PT + LWF state (NOT a separate state field)
uan                     varchar(20)          -- UAN (12-digit EPFO) — entered in edit screen, not wizard
pran                    varchar(20)          -- PRAN (NPS)
pf_wage_option          varchar(20)          -- 'Ceiling' | 'Actual' (Para 26(6))
is_epf_excluded         bool NOT NULL DEFAULT false
is_esi_excluded         bool NOT NULL DEFAULT false
is_lwf_excluded         bool NOT NULL DEFAULT false  -- per-employee LWF override
vpf_amount              decimal(18,4)        -- monthly voluntary PF addition
is_international_worker bool NOT NULL DEFAULT false
higher_eps_pension      bool NOT NULL DEFAULT false  -- SC 2022 ruling opt-in
```

#### `it_declarations` (employee's Form 12BB equivalent)

```sql
id                      uuid PK
employee_id             uuid NOT NULL FK
fiscal_year             varchar(10) NOT NULL
tax_regime              varchar(20) NOT NULL DEFAULT 'New'
other_income_declared   decimal(18,4) NOT NULL DEFAULT 0
prev_employer_salary    decimal(18,4) NOT NULL DEFAULT 0
prev_employer_tds       decimal(18,4) NOT NULL DEFAULT 0
submitted_at            timestamptz
created_at, created_by, updated_at, updated_by

UNIQUE(employee_id, fiscal_year)
```

---

## Phase 2: Payroll.Engine — Calculator Inputs & Outputs

### `EpfInputs` record

```csharp
public sealed record EpfInputs(
    bool IsEnabled,
    bool IsEmployeeExcluded,
    bool IsInternationalWorker,
    bool HigherEpsPensionOpted,      // SC 2022
    decimal BasicPlusDa,             // pre-LOP PF wage
    decimal PaidDays,
    decimal TotalDays,
    decimal WageCeilingOption,       // 15000 or decimal.MaxValue (Actual)
    decimal VpfAmount,
    bool ProRateByDays
);
```

### `EpfResult` record

```csharp
public sealed record EpfResult(
    decimal PfWage,                 // after pro-rata, after ceiling, after 50% rule
    decimal EmployeeEpf,            // 12% of PfWage
    decimal EmployeeVpf,
    decimal EmployerEpf,            // 3.67% residual (or adjusted for higher EPS)
    decimal EmployerEps,            // 8.33% of min(PfWage, 15000)
    decimal EmployerEdli,           // 0.50% capped at ₹75
    decimal EmployerAdminCharge,    // 0.50%, min ₹500
    int NcpDays
);
```

### `EsiInputs` record

```csharp
public sealed record EsiInputs(
    bool IsEnabled,
    bool IsNotifiedArea,
    bool IsEmployeeExcluded,
    decimal EligibilityWage,        // gross excl. OT (for ₹21,000 check)
    decimal ContributionWage,       // gross incl. OT (for rate calc)
    decimal DailyWage,              // for ₹176/day exemption check
    EsiCoverageStatus CoverageStatus,  // Covered | NotCovered | ContinuedPastThreshold
    decimal EligibilityThreshold,   // from DB config (₹21,000)
    decimal DailyEligibilityThreshold  // ₹176
);

public enum EsiCoverageStatus { Covered, NotCovered, ContinuedPastThreshold }
```

### `EsiResult` record

```csharp
public sealed record EsiResult(
    bool IsEligible,
    decimal EmployeeEsi,            // 0.75% of ContributionWage (or 0 if daily wage ≤ ₹176)
    decimal EmployerEsi             // 3.25% of ContributionWage
);
```

### `ProfessionalTaxInputs` record

```csharp
public sealed record ProfessionalTaxInputs(
    bool IsEnabled,
    bool IsEmployeeExcluded,
    string WorkStateCode,
    decimal GrossForPeriod,         // monthly gross for monthly states; accumulated for half-yearly/annual
    PtFrequency Frequency,
    Gender? EmployeeGender,         // required for Maharashtra
    int PayrollMonth,               // 1-12 (April=1); needed for February surcharge check
    List<PtSlab> StateSlabs         // loaded from DB for this state + effective date
);
```

### `TdsInputs` record

```csharp
public sealed record TdsInputs(
    bool IsEnabled,
    string FiscalYear,              // '2025-26'
    TaxRegime Regime,               // New (Old: DEFERRED)
    int PayrollMonth,               // 1=April .. 12=March
    decimal YtdGrossSalary,
    decimal CurrentMonthGross,
    decimal YtdTdsDeducted,
    decimal PrevEmployerSalary,
    decimal PrevEmployerTds,
    decimal EstimatedRemainingGross,
    decimal EmployerNpsAnnualActual,
    decimal BasicPlusDaAnnualProjected,  // for 80CCD(2) limit
    decimal YtdEmployerEpf,              // for ₹7.5L aggregate cap
    decimal OtherIncomeDeclared,
    bool HasPan,
    List<ItSlab> Slabs,
    List<ItSurchargeSlab> SurchargeSlabs,
    ItConfig Config                 // standard deduction, 87A params
);
```

### `TdsResult` record

```csharp
public sealed record TdsResult(
    decimal ProjectedAnnualGross,
    decimal TaxableIncome,
    decimal GrossTax,
    decimal Surcharge,
    decimal Rebate87A,
    decimal Cess,
    decimal TotalAnnualTax,
    decimal AlreadyDeducted,
    decimal RemainingObligation,
    decimal MonthlyTds,             // this month's deduction
    decimal ExcessTds               // if overpaid (TDS = 0 this month, excess logged)
);
```

---

## Phase 3: Engine Calculator Logic (Key Algorithms)

### EPF Calculator

```csharp
// Pseudo-code
decimal pfWage = proRateByDays
    ? Round(basicPlusDa / totalDays * paidDays, 2)
    : basicPlusDa;

// 50% wage rule (Labour Codes 2025)
// Called from salary structure resolution layer before engine:
// if (excludedComponents > 0.5 * totalCtc) pfWage += excess

decimal ceilingWage = isInternationalWorker ? pfWage : Min(pfWage, 15000);
decimal epsWage = Min(ceilingWage, 15000);

decimal employeeEpf = Round(ceilingWage * 0.12m, 0, MidpointRounding.AwayFromZero);
decimal employeeVpf = vpfAmount;

decimal employerEps = Round(epsWage * 0.0833m, 0, MidpointRounding.AwayFromZero);
decimal employer12Pct = Round(ceilingWage * 0.12m, 0, MidpointRounding.AwayFromZero);
decimal employerEpf = employer12Pct - employerEps;  // residual; min 0

decimal employerEdli = Min(Round(ceilingWage * 0.005m, 0), 75);
decimal employerAdmin = Max(Round(ceilingWage * 0.005m, 0), 500);
// Account 22 (EDLI admin): ZERO — waived Apr 2017
```

### ESI Calculator

```csharp
bool eligible = coverageStatus != EsiCoverageStatus.NotCovered
    && isNotifiedArea
    && !isEmployeeExcluded;

if (!eligible) return EsiResult.Zero;

decimal employeeRate = dailyWage <= 176 ? 0m : 0.0075m;
decimal employeeEsi = Round(contributionWage * employeeRate, 0, MidpointRounding.AwayFromZero);
decimal employerEsi = Round(contributionWage * 0.0325m, 0, MidpointRounding.AwayFromZero);
```

### ESI Period Coverage Evaluator (Application layer, not Engine)

```csharp
// Called at start of each contribution period (April 1, October 1)
// and when a new employee joins
EsiCoverageStatus EvaluateCoverage(decimal eligibilityWage, decimal threshold, 
    EsiCoverageStatus currentStatus, bool isPeriodBoundary)
{
    if (isPeriodBoundary)
        return eligibilityWage <= threshold 
            ? EsiCoverageStatus.Covered 
            : EsiCoverageStatus.NotCovered;

    // Mid-period: if was covered, continue regardless of wage crossing threshold
    return currentStatus == EsiCoverageStatus.Covered
        ? EsiCoverageStatus.ContinuedPastThreshold  // still contributes
        : EsiCoverageStatus.NotCovered;
}
```

### PT Calculator

```csharp
// For Monthly frequency:
decimal pt = LookupSlab(stateSlabs, grossForPeriod, employeeGender);
if (isFebruarySurcharge && payrollMonth == 11)  // February = month 11 in FY (April=1)
    pt = LookupSurcharge(stateSlabs, grossForPeriod, employeeGender);

// For HalfYearly frequency:
// Application layer accumulates grossForPeriod over Apr-Sep or Oct-Mar
// Engine receives the 6-month total and looks up half-yearly slab

// For Annual frequency:
// Application layer tracks YTD gross, engine receives projected annual
// PT split: (ptAnnual / 11) for months 1-11, remainder in month 12
```

### TDS Calculator (core algorithm)

```csharp
// Step 1: Projected annual gross
decimal monthsElapsed = payrollMonth;
decimal monthsRemaining = 13 - payrollMonth;  // inclusive of current month
decimal projectedAnnual = prevEmployerSalary
    + ytdGrossSalary
    + currentMonthGross
    + estimatedRemainingGross;

// Step 2: Standard deduction (not prorated)
decimal afterStdDeduction = projectedAnnual - config.StandardDeduction;  // ₹75,000

// Step 3: Other income
decimal grossTotalIncome = afterStdDeduction + otherIncomeDeclared;

// Step 4: 80CCD(2) — employer NPS
decimal nps80CCD2 = Min(employerNpsAnnualActual, basicPlusDaAnnualProjected * config.NpsMaxRate);
// Also check ₹7.5L aggregate cap
decimal aggregateEmployerStatutory = ytdEmployerEpf * 12m / monthsElapsed + nps80CCD2;
// If aggregate > 7.5L, excess = taxable perquisite (add back to grossTotalIncome)
if (aggregateEmployerStatutory > config.EmployerStatutoryCap)
    grossTotalIncome += aggregateEmployerStatutory - config.EmployerStatutoryCap;

decimal taxableIncome = Max(0, grossTotalIncome - nps80CCD2);

// Step 5: Slab tax
decimal tax = ComputeSlabTax(taxableIncome, slabs);

// Step 6: Surcharge + marginal relief
decimal surcharge = ComputeSurcharge(taxableIncome, tax, surchargeSlabs);

// Step 7: 87A rebate
decimal taxPlusSurcharge = tax + surcharge;
decimal rebate = Compute87A(taxableIncome, taxPlusSurcharge, config);

// Step 8: Cess
decimal totalTax = Round((taxPlusSurcharge - rebate) * 1.04m, 0, MidpointRounding.AwayFromZero);
if (taxableIncome <= 0) totalTax = 0;

// No PAN: max(totalTax, projectedAnnual * 0.20)
if (!hasPan) totalTax = Max(totalTax, projectedAnnual * 0.20m);

// Step 9: Monthly installment
decimal alreadyDeducted = ytdTdsDeducted + prevEmployerTds;
decimal remaining = Max(0, totalTax - alreadyDeducted);
decimal monthlyTds = Ceiling(remaining / monthsRemaining);

// If overdeducted
decimal excess = Max(0, alreadyDeducted - totalTax);
```

### 87A Rebate + Marginal Relief

```csharp
decimal Compute87A(decimal taxableIncome, decimal taxPlusSurcharge, ItConfig config)
{
    if (taxableIncome <= config.Rebate87ALimit)  // ₹12,00,000
        return Min(taxPlusSurcharge, config.Rebate87AAmount);  // ₹60,000

    // Marginal relief zone: ₹12,00,001 to ₹12,75,000
    if (taxableIncome <= config.Rebate87ALimit + config.StandardDeduction)
    {
        decimal maxPayable = taxableIncome - config.Rebate87ALimit;
        return Max(0, taxPlusSurcharge - maxPayable);
    }

    return 0;
}
```

---

## Phase 4: Application Layer

### Commands

| Command | Handler responsibility |
|---------|----------------------|
| `ConfigureEpfCommand` | Save org EPF settings; validate establishment code format |
| `ConfigureEsiCommand` | Save org ESI settings; validate establishment code |
| `ConfigurePtCommand` | Enable PT for org; validate state slabs loaded |
| `ConfigureLwfCommand` | Enable LWF for org; validate state config loaded |
| `SubmitItDeclarationCommand` | Create/update `it_declarations` for employee + FY |
| `UpdateEmployeeStatutoryProfileCommand` | Update UAN, PRAN, work state, PF option, VPF |

### Queries

| Query | Returns |
|-------|---------|
| `GetStatutoryConfigQuery` | Full org statutory config DTO |
| `GetEmployeeStatutoryProfileQuery` | Employee UAN, PRAN, work state, flags |
| `GetProfessionalTaxSlabsQuery` | Slabs for given state + effective date |
| `GetLwfRateQuery` | LWF config for given state + effective date |
| `GetIncomeTaxConfigQuery` | Slabs + config for given FY + regime |

---

## Phase 5: API Endpoints

```
-- Statutory Components (Settings > Statutory Components)
GET    /api/v1/statutory/config                          → GetStatutoryConfigQuery
PUT    /api/v1/statutory/epf                             → ConfigureEpfCommand
PUT    /api/v1/statutory/esi                             → ConfigureEsiCommand
PUT    /api/v1/statutory/pt                              → ConfigurePtCommand
PUT    /api/v1/statutory/lwf/{stateCode}/enable          → EnableLwfStateCommand
PUT    /api/v1/statutory/lwf/{stateCode}/disable         → DisableLwfStateCommand
PUT    /api/v1/statutory/statutory-bonus                 → ConfigureStatutoryBonusCommand

-- Tax Settings (Settings > Taxes — separate from above)
GET    /api/v1/settings/taxes                            → GetTaxSettingsQuery
PUT    /api/v1/settings/taxes                            → UpdateTaxSettingsCommand
PUT    /api/v1/settings/it-declaration-window            → UpdateItDeclarationWindowCommand

-- Work Locations (Settings > Work Locations)
GET    /api/v1/work-locations                            → ListWorkLocationsQuery
POST   /api/v1/work-locations                            → CreateWorkLocationCommand
PUT    /api/v1/work-locations/{id}                       → UpdateWorkLocationCommand
DELETE /api/v1/work-locations/{id}                       → DeleteWorkLocationCommand

-- Employee statutory profile
GET    /api/v1/employees/{id}/statutory                  → GetEmployeeStatutoryProfileQuery
PUT    /api/v1/employees/{id}/statutory                  → UpdateEmployeeStatutoryProfileCommand

-- IT Declaration
GET    /api/v1/employees/{id}/it-declaration/{fy}       → GetItDeclarationQuery
POST   /api/v1/employees/{id}/it-declaration             → SubmitItDeclarationCommand

-- Config lookups (used by engine at payroll run time)
GET    /api/v1/statutory/pt-slabs?state={code}          → GetProfessionalTaxSlabsQuery
GET    /api/v1/statutory/lwf-rate?state={code}          → GetLwfRateQuery
GET    /api/v1/statutory/it-config?fy={year}            → GetIncomeTaxConfigQuery
```

---

## Phase 6: Frontend — Pages & UI (Zoho Parity)

### 6a. StatutoryComponentsPage

**Route:** `/settings/statutory`  
**Layout:** Horizontal tab strip — **EPF | ESI | Professional Tax | LWF | Statutory Bonus**

> TDS/Tax is a separate page at `/settings/taxes`. It is NOT a tab here.

#### EPF Tab

> View mode shows all fields read-only with pencil edit icon. Edit form is a separate screen at `/settings/statutory-details/edit-epf-details`. Same pattern for ESI.

**Edit form fields (exact Zoho layout, left column + right panel):**

Left column:
- **EPF Number** — text input, placeholder `AA/AAA/0000000/XXX`, format hint below
- **Deduction Cycle** — read-only "Monthly" (system-fixed)
- **Employee Contribution Rate** — dropdown: "12% of Actual PF Wage"
- **Employer Contribution Rate** — dropdown: "12% of Actual PF Wage" + **"View Splitup"** link (shows EPF/EPS/EDLI breakdown modal)
- **Include employer's contribution in employee's salary structure.** — checkbox (primary; checked by default)
  - Sub (visible only if above checked): **Include employer's EDLI contribution in employee's salary structure.** — checkbox + ⓘ
  - Sub (visible only if above checked): **Include admin charges in employee's salary structure.** — checkbox + ⓘ
- **Override PF contribution rate at employee level** — checkbox (enables per-employee rate override)
- **PF Configuration when LOP Applied** — section label
  - **Pro-rate Restricted PF Wage** — checkbox; helper: "PF contribution will be pro-rated based on the number of days worked by the employee."
  - **Consider all applicable salary components if PF wage is less than ₹15,000 after Loss of Pay** — checkbox (checked by default); helper: "PF wage will be computed using the salary earned in that particular month (based on LOP) rather than the actual amount mentioned in the salary structure."
- **Save** + **Cancel** buttons

Right panel (live preview, updates dynamically):
- **Sample EPF Calculation** — assumes ₹20,000 PF wage; shows: Employee EPF | Employer EPS | Employer EPF | Total
- **Preview EPF Calculation** button → multi-scenario preview modal

View mode bottom action: **Disable EPF** link (not a toggle — EPF must be explicitly disabled)

#### ESI Tab

View mode fields (pencil → edit screen at `/settings/statutory-details/edit-esi-details`):
- **ESI Number** — text input (format: `52-00-XXXXXX-000-0001`)
- **Deduction Cycle** — read-only "Monthly"
- **Employees' Contribution** — read-only "0.75% of Gross Pay"
- **Employer's Contribution** — read-only "3.25% of Gross Pay"

View mode bottom action: **Disable ESI** link

#### Professional Tax Tab

> No org-level enable/disable. No single registration field. Shows **one card per work location**.

**Layout:**
- Page heading: "Professional Tax"
- Subtitle: "This tax is levied on an employee's income by the State Government. Tax slabs differ in each state."
- One **card per work location** (e.g. "Head Office"). Each card:
  - **PT Number** — "Update PT Number" link (click → inline text input to enter registration)
  - **State** — read-only, derived from work location's state (e.g. "Kerala")
  - **Deduction Cycle** — read-only, auto-set by state (e.g. "Half Yearly")
  - **PT Slabs** — "View Tax Slabs" link (opens modal with slab table) + "(Revise)" link

**Revise PT Slabs screen** (separate page, e.g. `/settings/statutory-details/{id}/pt-details`):
- **Deduction Cycle** — read-only dropdown (pre-filled by state)
- **Tax Slabs based on {Frequency} Gross Salary** — editable table:
  - Columns: START RANGE (₹) | — | END RANGE (₹) | {FREQUENCY} TAX AMOUNT (₹) | × (delete row)
  - First row start range is disabled (always 1)
  - All other fields are editable spinbuttons
  - × buttons to delete rows (first row has no × )
  - **+ Additional Slab** button
- **Effective From** — date picker (dd/MM/yyyy)
- **Save** + **Cancel**

#### LWF Tab

> Same card-per-work-location pattern as PT. Only states relevant to the org's work locations appear.

**Layout:**
- Page heading: "Labour Welfare Fund"
- Subtitle: "Labour Welfare Fund act ensures social security and improves working conditions for employees."
- One **card per work location state** that has LWF. Each card:
  - **State name** as card title (e.g. "Kerala")
  - **Employees' Contribution** — read-only (e.g. "₹50.00")
  - **Employer's Contribution** — read-only (e.g. "₹50.00")
  - **Deduction Cycle** — read-only (e.g. "Monthly")
  - **Status** — "Enabled **(Disable)**" or "Disabled **(Enable)**" — the parenthetical is a clickable link, fires immediately (no Save)

> States not in the org's work locations do not appear at all.

#### Statutory Bonus Tab

Fields in order:
- **Enable Statutory Bonus** button (top, toggles the entire feature)
- When enabled:
  - **Payment Frequency** dropdown: Monthly | Yearly
  - **Bonus Percentage** numeric input with helper "Must be between 8.33% and 20%"
  - **Minimum Wage** section:
    - "+ Add Minimum Wage" button → adds row: State (dropdown) + Minimum Wage (₹ input)
    - One row per state where org has employees
- **Save** button

---

### 6b. TaxesPage (Settings > Taxes > Tax Details)

**Route:** `/settings/taxes`  
**Sidebar**: "Taxes" collapsible group → "Tax Details" child item

#### Organisation Tax Details section
- **PAN*** — text input, format `AAAAA0000A` (required)
- **TAN** — text input, format `AAAA00000A`
- **TDS circle / AO code** ⓘ — 4 segmented inputs: `AAA / AA / 000 / 00`
- **Tax Payment Frequency** ⓘ — "Monthly" (read-only in edit; set during org setup)

#### Tax Deductor Details section
- **Deductor's Type** — radio: **Employee** | Non-Employee
- If Employee:
  - **Deductor's Name*** — employee dropdown (auto-fills father's name)
  - **Deductor's Father's Name*** — text, auto-populated from selected employee
- If Non-Employee:
  - **Deductor's Name***, **Deductor's Designation**, **Deductor's Father's Name***
- **Save** button

> IT Declaration window settings (Release IT Declaration, POI lock date, email reminders) live in Settings > Preferences or General, not here. Add to Settings > General module in V1.

---

### 6c. WorkLocationsPage (Settings > Work Locations)

**Route:** `/settings/work-locations`  
**Sidebar:** Under Organisation group

This page is foundational — PT and LWF state derive from work location state.

**List view** — card layout (not table). Each card shows:
- Work Location Name (card title)
- Org name
- Address line
- City, State, PIN Code
- Employee count ("N Employees")
- **FILING ADDRESS** badge (teal) — shown on the primary/first location
- Pencil edit icon + … more actions (edit/delete)

**"Add Work Location"** button (top right) → form:
- **Work Location Name*** (required)
- **Address*** (required):
  - Address Line 1
  - Address Line 2
  - **State** dropdown (listed before City — matches Zoho field order)
  - City
  - PIN Code
- **Save** + **Cancel**

> No explicit "primary" toggle. "FILING ADDRESS" auto-applied to first location. Additional locations can be designated filing address via … menu.

---

### 6d. Employee Statutory Profile (Employee Edit Screen)

**Location:** Employee detail page → Overview section or dedicated "Statutory" section

Fields (shown after employee created — NOT in the 4-step wizard):
- **UAN** (12-digit EPFO) text input
- **PF Account Number** text input
- **Contribute EPS at actual PF wages** toggle (visible only if org EPF enabled + EPS actual wages toggle on)
- **ESI IP Number** text input
- **VPF Amount** (₹/month) numeric input
- **LWF Override** — toggle to exclude this employee from LWF

---

### 6e. Add Employee Wizard — Statutory fields (Step 1)

In The Basics step, matching Zoho:
- **Work Location** dropdown (linked to `/settings/work-locations`) — drives PT + LWF state
- **Statutory Components** section:
  - Enable PF toggle
  - Enable ESI toggle

---

### 6f. IT Declaration (Employee Self-Service)

**Route (employee portal or admin proxy view):** `/employees/{id}/it-declaration`

4 sections matching Zoho's declaration flow:
1. **House Rent** — rental period, monthly rent, address, metro flag, landlord PAN (required if rent > ₹1L/yr)
2. **Home Loan** — projected principal, projected interest, lender name, lender PAN
3. **Deductions** — Section 80C (₹1.5L limit): LIC, MF, NABARD, ULIP, etc.; Section 80D mediclaim
4. **Other Sources of Income** — non-employment income; prior employer salary + TDS

**"Submit and Compare"** CTA → Tax comparison page showing New vs Old regime (V1: always New only; comparison page DEFERRED until old regime implemented).

---

### 6g. Payslip Format (statutory line items, exact labels)

Earnings section:
- Basic Pay
- Dearness Allowance
- House Rent Allowance
- [Other earning components]

Deductions section (exact payslip labels):
- **Employees' Provident Fund**
- **Employees' State Insurance**  
- **Professional Tax**
- **Labour Welfare Fund**
- **Tax Deducted at Source**

Employer contributions section (separate from deductions):
- Employer Provident Fund
- Employer ESI

---

## Phase 7: Seed Data

All DB config data seeded at tenant provision time via `StatutorySlabSeeder`:

### PT Slabs (FY2025-26 for all 21 states + Puducherry)

Full slab table per state — see research findings above. Key ones:

| State | Frequency | Threshold / Slab | Amount |
|-------|-----------|-----------------|--------|
| Karnataka | Monthly | < ₹25,000 | Nil; ≥ ₹25,000 | ₹200 (₹300 Feb) |
| Maharashtra | Monthly | Male < ₹7,500 | Nil; ₹7,501-10,000 | ₹175; > ₹10,000 | ₹200 (₹300 Feb) |
| Maharashtra | Monthly | Female < ₹25,000 | Nil; > ₹25,000 | ₹200 (₹300 Feb) |
| Kerala | HalfYearly | 8 slabs — nil to ₹1,250/half-year | See research |
| Tamil Nadu | HalfYearly | 6 slabs — nil to ₹1,250/half-year | See research |
| West Bengal | Monthly | 5 slabs — nil to ₹200/month | See research |
| Andhra Pradesh | Monthly | < ₹15,000 nil; ₹15-20k ₹150; > ₹20k ₹200 | |
| Gujarat | Monthly | < ₹12,000 nil; ≥ ₹12,000 ₹200 | |
| Bihar | Annual | 4 slabs, max ₹2,500 | |
| Jharkhand | Annual | 5 slabs, max ₹2,500 | |
| ... | ... | ... | |

### LWF Rates (all 16 states)

Seed with `effective_date = 2024-01-01` for recent revisions (West Bengal Jan 2024), `2024-03-01` for Maharashtra, `2025-01-01` for Karnataka.

### Income Tax Config (FY2025-26)

```
Standard deduction: ₹75,000
87A rebate limit: ₹12,00,000
87A rebate amount: ₹60,000
Employer statutory cap: ₹7,50,000
NPS employer max rate: 14%

Slabs (new regime):
  0 – 4,00,000: 0%
  4,00,001 – 8,00,000: 5%
  8,00,001 – 12,00,000: 10%
  12,00,001 – 16,00,000: 15%
  16,00,001 – 20,00,000: 20%
  20,00,001 – 24,00,000: 25%
  24,00,001+: 30%

Surcharge slabs:
  0 – 50,00,000: 0%
  50,00,001 – 1,00,00,000: 10%
  1,00,00,001 – 2,00,00,000: 15%
  2,00,00,001 – 5,00,00,000: 25%
  5,00,00,001+: 25%  ← capped at 25% for new regime (old regime was 37%)
```

---

## Phase 8: Payroll Run Integration

### Payroll run order of operations (full sequence)

```
1.  Lock attendance inputs (LOP days, OT hours per employee)
2.  Resolve employee state (joining date, exit date, active salary structure)
3.  Compute monthly gross from salary structure:
      - Fixed components × (paidDays / totalDays) if pro-ratable
      - Fixed components at full if not pro-ratable
      - Reimbursements at full (never pro-rated)
      - Variable inputs (OT, bonus) added
4.  Apply Labour Code 50% wage check:
      - Sum excluded components (HRA + allowances excl. Basic+DA)
      - If > 50% of CTC, add excess to PF wage
5.  Compute EPF (EpfCalculator)
6.  Evaluate ESI coverage status (period boundary check or new joiner check)
7.  Compute ESI (EsiCalculator)
8.  Compute PT (ProfessionalTaxCalculator — for half-yearly/annual states, accumulate first)
9.  Compute LWF (LwfCalculator — apply on deduction month only for non-monthly)
10. Compute TDS (TdsCalculator — full annual projection recalc)
11. Sum employee deductions: EPF + VPF + ESI + PT + LWF + TDS + advances + loan recovery
12. Net pay = gross earnings - employee deductions
13. Employer contributions: employer EPF + EPS + EDLI + admin + employer ESI + employer NPS
14. CTC check: gross + employer contributions + gratuity accrual = budgeted CTC (flag if off)
15. Lock payroll run row
```

### What feeds into ECR 2.0 file

After payroll run locked, ECR generator reads:
- `uan` (employee)
- `pf_wage` (from EpfResult)
- `eps_wage = min(pf_wage, 15000)`
- `employer_epf_share` (from EpfResult)
- `eps_contribution` (from EpfResult)
- `epf_eps_diff = employee_epf - eps_contribution` (per ECR spec)
- `ncp_days` (attendance LOP days)
- `gross_wages` (total monthly gross)
- `refund_of_advances` (any PF advance repayment that month)

---

## Phase 8b: Zoho Parity — Additional UX Flows

### Org Setup Wizard (first-run, 5 steps)

Zoho runs a 5-step setup wizard before first payroll. We will implement this as a guided setup for new tenants:

| Step | Content |
|------|---------|
| 1 | Organization Setup — name, business location, address, industry, prior payroll history flag |
| 2 | Tax Information — PAN, TAN, AO Code, Tax Payment Frequency |
| 3 | Pay Schedule — work week, salary basis (calendar days vs. fixed 30), payment date |
| 4 | Statutory Components — EPF (all toggles), ESI (establishment code), PT (registration), LWF (per-state enable), Statutory Bonus |
| 5 | Salary Components — configure earnings, benefits, deductions, reimbursements |

V1 implementation: Steps 4 and 5 map directly to Settings > Statutory Components + Settings > Salary Components (already built). Step 2 maps to Settings > Taxes. Wizard is a guided nav through existing settings pages.

### Salary Revision Flow

Post-onboarding salary change. Zoho fields:
- **Revised Annual CTC** (or % increase)
- **Per-component amounts** (revision)
- **Effective From** (month — drives arrears start date)
- **Payout Month** (month — when revised + arrears are paid)
- Arrears auto-computed: `delta × (payout_month − effective_from)`
- Approval workflow: Submit → Approver → Final Approve → auto-applies in that month's pay run

### Employee Profile Tabs (Admin View)

| Tab | Contents |
|-----|---------|
| Overview | Name, designation, email, department, work location, UAN, PF account, basic statutory info |
| Salary Details | Template, CTC breakdown, payslip history, Revise Salary button |
| Tax | TDS worksheets, month-wise TDS, Form 16 |
| Personal Info | DOB, father's name, PAN, Aadhaar (masked), residential address |
| Payment Info | Payment method, bank account |
| IT Declaration | Investment declarations + POI |

---

## Phase 9: Deferred to V2

These are out of scope for V1 but plan for data model hooks:

| Feature | Deferred reason |
|---------|----------------|
| Old tax regime | V1 constraint — new regime only |
| Statutory Bonus computation engine | Complex surplus/set-on/set-off; lump-sum separate run |
| Gratuity terminal settlement | Requires exit management module |
| Form 24Q filing | Needs TAN integration, TRACES API |
| Form 16 generation | Needs TRACES Part A download integration |
| ECR file auto-upload | Needs EPFO portal API/integration |
| ESIC return filing | Needs ESIC portal integration |
| Higher EPS pension (SC 2022) | Retrospective; edge case |
| International Workers | Edge case |
| Set-on / set-off bonus accounting | Requires multi-year surplus tracking |
| Labour Code 50% wage enforcement UI | Phase 2 — flag in reports first |

---

## Implementation Order

```
Sprint 1 (DB + Seed):
  □ Migration: AddWorkLocationsTable
  □ Migration: AddStatutoryTables (statutory_org_config, pt_slabs, lwf_state_config, it_slabs, it_config)
  □ Add statutory fields to employees table (work_location_id FK, uan, pran, pf_wage_option, is_epf_excluded, is_esi_excluded, is_lwf_excluded, vpf_amount)
  □ StatutorySlabSeeder (PT all states, LWF all states, IT FY2025-26)
  □ Seed runs at tenant provision (update TenantSchemaProvisioner)
  □ Verify: new tenant gets all seed data; existing tenants get migration applied

Sprint 2 (Engine):
  □ EpfCalculator + unit tests (95% coverage)
  □ EsiCalculator + unit tests
  □ ProfessionalTaxCalculator + unit tests (monthly, half-yearly, annual, Feb surcharge)
  □ LwfCalculator + unit tests
  □ TdsCalculator + unit tests (edge cases: 87A marginal relief, surcharge relief, no PAN, mid-year join, Dec join, excess TDS)

Sprint 3 (Application + API):
  □ WorkLocation entity + repository + CRUD commands + ListWorkLocationsQuery
  □ StatutoryOrgConfig entity + repository
  □ Configure EPF command + handler
  □ Configure ESI command + handler
  □ Configure PT command + handler (updates PT registration number)
  □ EnableLwfState / DisableLwfState commands + handlers
  □ ConfigureStatutoryBonus command + handler
  □ UpdateEmployeeStatutoryProfile command (UAN, PF option, VPF, ESI/LWF exclusions)
  □ SubmitItDeclaration command + handler
  □ UpdateTaxSettings command (TAN, PAN, AO Code)
  □ UpdateItDeclarationWindow command
  □ Controllers: StatutoryComponentsController, WorkLocationsController, TaxSettingsController
  □ API integration tests

Sprint 4 (Frontend):
  □ WorkLocationsPage — Settings > Work Locations (list + add/edit form with state dropdown)
  □ StatutoryComponentsPage tab shell (EPF | ESI | Professional Tax | LWF | Statutory Bonus)
  □ EpfTab — all toggles matching Zoho field order
  □ EsiTab — establishment code + read-only rates
  □ PtTab — registration number + state slab table
  □ LwfTab — per-state table with Enable/Disable per row
  □ StatutoryBonusTab — enable toggle + frequency + % + per-state minimum wages
  □ TaxesPage — Settings > Taxes (tax deductor details + IT declaration window)
  □ Employee statutory section on employee detail page (UAN, PRAN, PF option, VPF, ESI/LWF overrides)
  □ Add Employee wizard Step 1: Work Location selector + statutory enables
  □ IT declaration form (Section 80C/D + HRA + home loan + other income)

Sprint 5 (Payroll Run Integration):
  □ Wire all calculators into payroll run execution engine
  □ ESI period coverage evaluator (period boundary detection)
  □ LWF deduction scheduler (apply only on correct months per state)
  □ ECR 2.0 file generator
  □ End-to-end payroll run test with all statutory outputs verified
```

---

## Key Design Decisions

1. **All statutory rates in DB — never hardcoded.** PT slabs, LWF amounts, IT slabs, ESI threshold, EPF ceiling — all pulled from config tables at runtime. Rate changes = DB update, no code deploy.

2. **Engine is pure.** `TdsCalculator`, `EpfCalculator`, etc. take only value inputs and return deterministic outputs. No EF, no Redis, no async. Testable without any infrastructure.

3. **ESI needs two wage values per employee per month** — eligibility wage (excl. OT) and contribution wage (incl. OT). Not one.

4. **PT and LWF state derives from Work Location, not a direct employee field.** Employees are assigned a work location; work location carries the state. This is the Zoho model and is legally correct (PT/LWF are workplace-jurisdiction). Employee cannot have a separate `work_state_code` — it reads from `work_location.state_code`.

4a. **PT and LWF UI shows cards per work location, not a flat 16-state table.** Only states where the org has a work location appear. PT slab revise navigates to a dedicated page with an inline-editable slab table + Effective From date. No Save on the LWF enable/disable — each state toggle fires immediately.

5. **TDS is in Settings > Taxes, not Settings > Statutory Components.** Statutory Components tabs are: EPF | ESI | Professional Tax | LWF | Statutory Bonus. TDS configuration (TAN, PAN, AO Code, IT declaration window) lives at `/settings/taxes`.

6. **PT has no org-level enable/disable.** It is always active once PT registration is entered. Zoho explicitly states PT cannot be disabled org-wide.

7. **LWF uses per-state enable/disable rows** — not a single org toggle. Admin enables/disables each state independently.

8. **UAN and statutory per-employee fields go on the edit screen, not the Add Employee wizard.** The wizard only captures: work location (for PT/LWF state) + EPF/ESI enables. UAN, PRAN, VPF, overrides are entered after onboarding via the employee edit page.

9. **TDS recalculates annually from scratch every month.** Never incremental. `CEILING(remaining / monthsRemaining)` — last month absorbs rounding.

10. **Gratuity = CTC provision line only in V1.** No terminal settlement computation in V1 — that requires exit management module.

11. **Fixed Allowance (ResidualCTC) in salary structure is the plug** — it absorbs CTC that doesn't go to named components. Under 50% wage rule, if allowances exceed 50% of CTC, residual is reclassified toward PF wage.

12. **All monetary values: `decimal`.** Tax amounts, rates, percentages — zero exceptions. All rounding uses `MidpointRounding.AwayFromZero` (standard ESIC/EPFO rounding).
