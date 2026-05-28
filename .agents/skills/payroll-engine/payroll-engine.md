# Skill: Payroll Engine

## What This Covers

`src/Payroll.Engine/` — standalone C# class library.
Pure functions, decimal arithmetic, zero I/O, zero DI, zero async.
Called by Hangfire worker after inputs loaded from DB.

---

## Project Constraints

```xml
<!-- Payroll.Engine.csproj — no framework references allowed -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <!-- NO EF Core, NO ASP.NET, NO Redis, NO HttpClient -->
</Project>
```

---

## Module Structure

```
Payroll.Engine/
  Inputs/
    EmployeeInput.cs         # all employee data needed for a run
    PayrollRunInput.cs       # run-level config (period, tenant config)
    StatutoryConfig.cs       # tax slabs, PF limits, ESI limits — loaded from DB, passed in
  Outputs/
    PayrollResult.cs         # per-employee result
    TDSResult.cs
    PFResult.cs
    ESIResult.cs
    PTResult.cs
    LWFResult.cs
  Calculators/
    GrossCalculator.cs
    TDSCalculator.cs
    PFCalculator.cs
    ESICalculator.cs
    PTCalculator.cs
    LWFCalculator.cs
    FnFCalculator.cs
  PayrollEngine.cs           # orchestrates all calculators
```

---

## Input Design Pattern

All statutory config (slabs, rates, limits) is passed in — never fetched internally:

```csharp
public sealed record StatutoryConfig(
    IReadOnlyList<TaxSlab> NewRegimeSlabs,   // FY-specific, loaded from DB
    decimal PFWageLimit,                      // ₹15,000 or custom
    decimal ESIWageLimit,                     // ₹21,000
    decimal ESIEmployeeRate,                  // 0.75%
    decimal ESIEmployerRate,                  // 3.25%
    IReadOnlyList<PTSlab> PTSlabs,            // state-specific
    decimal? LWFEmployeeAmount,
    decimal? LWFEmployerAmount
);
```

---

## Calculator Pattern

Every calculator is a static class or a sealed class with no state:

```csharp
public static class TDSCalculator
{
    // All inputs explicit. No hidden state. No I/O.
    public static TDSResult Compute(
        decimal annualGross,
        decimal priorEmployerTDSDeducted,    // for mid-year joiners
        decimal priorEmployerTaxableIncome,
        IReadOnlyList<TaxSlab> slabs,
        int monthsRemaining)
    {
        // 1. Project annual taxable income
        // 2. Apply standard deduction (₹75,000 new regime FY2026)
        // 3. Apply slabs
        // 4. Add cess (4%)
        // 5. Subtract prior TDS
        // 6. Spread over remaining months
        // Returns: monthly TDS amount, annual tax liability, effective rate
    }
}
```

---

## Decimal Arithmetic Rules

```csharp
// ALWAYS round at final output, not intermediate steps
// Use RoundingMode: MidpointRounding.AwayFromZero for statutory amounts

decimal annualTax = Math.Round(taxBeforeRound, 0, MidpointRounding.AwayFromZero);
decimal monthlyTds = Math.Round(annualTax / monthsRemaining, 0, MidpointRounding.AwayFromZero);

// PF: round to nearest rupee
decimal pfEmployee = Math.Round(pfWage * 0.12m, 0, MidpointRounding.AwayFromZero);

// ESI: round to nearest paisa (2 decimal places)
decimal esiEmployee = Math.Round(grossWage * 0.0075m, 2, MidpointRounding.AwayFromZero);
```

---

## Statutory Calculations Reference

### TDS (New Regime FY2026)
```
Standard deduction:  ₹75,000
Tax-free slab:       ₹0 – ₹3,00,000 @ 0%
Slab 1:              ₹3,00,001 – ₹7,00,000 @ 5%
Slab 2:              ₹7,00,001 – ₹10,00,000 @ 10%
Slab 3:              ₹10,00,001 – ₹12,00,000 @ 15%
Slab 4:              ₹12,00,001 – ₹15,00,000 @ 20%
Slab 5:              above ₹15,00,000 @ 30%
Surcharge:           10% for income > ₹50L, 15% for > ₹1Cr
Cess:                4% of (tax + surcharge)
Rebate u/s 87A:      ₹25,000 if income ≤ ₹7,00,000 (after std deduction)
```
All slabs stored in DB — engine receives them as `StatutoryConfig.NewRegimeSlabs`. Never hardcode.

### PF / EPF / EPS / EDLI
```
EPF employee:        12% of PF wage (capped at ₹15,000 unless uncapped by employer)
EPF employer:        3.67% of PF wage (balance after EPS)
EPS employer:        8.33% of PF wage (capped at ₹1,250/month)
EDLI employer:       0.5% of PF wage (capped at ₹75/month)
Admin charge:        0.5% of PF wage (min ₹500/month)
EDLI inspection:     Not applicable from 2020 — enforce zero
```

### ESI
```
Employee:  0.75% of gross wages
Employer:  3.25% of gross wages
Threshold: gross > ₹21,000/month = ESI exempt (stop mid-period per contribution period rules)
```

### PT (state-wise — pulled from DB)
```
Example Karnataka: ₹200/month for salary > ₹15,000
PT is deductible from taxable income for TDS
```

### LWF (state-wise — pulled from DB)
```
Example Maharashtra: ₹6 employee + ₹12 employer (paid monthly)
```

---

## Mid-Year Joiner / Prior Employer YTD

Engine must handle prior employer YTD:

```csharp
public sealed record PriorEmployerYTD(
    decimal TaxableIncome,
    decimal TDSDeducted,
    decimal PFContributed,
    int MonthsWorked
);
```

TDS calculation: total annual tax = project full-year income from current + prior → subtract prior TDS → spread over remaining months.

---

## Proration Rules

- Mid-month joiner/leaver: prorate on calendar days in pay period.
- Loss of Pay (LOP): prorate gross on (working days - LOP days) / working days.
- `decimal` proration: `Math.Round(amount * daysWorked / totalDays, 2)`.

---

## Engine Orchestration

```csharp
public static class PayrollEngine
{
    public static IReadOnlyList<PayrollResult> Compute(
        IReadOnlyList<EmployeeInput> employees,
        PayrollRunInput runInput,
        StatutoryConfig config)
    {
        return employees
            .Select(emp => ComputeOne(emp, runInput, config))
            .ToList();
    }

    private static PayrollResult ComputeOne(
        EmployeeInput emp,
        PayrollRunInput run,
        StatutoryConfig config)
    {
        var gross = GrossCalculator.Compute(emp, run);
        var pt    = PTCalculator.Compute(gross.TaxableGross, config.PTSlabs, emp.State);
        var pf    = PFCalculator.Compute(gross.PFWage, config, emp.PFOptOut);
        var esi   = ESICalculator.Compute(gross.GrossWage, config, emp.ESIExempt);
        var tds   = TDSCalculator.Compute(gross.AnnualProjectedGross, pt, pf,
                        emp.PriorEmployerYTD, config, run.MonthsRemaining);
        var lwf   = LWFCalculator.Compute(emp.State, config, run.PayPeriod);
        var net   = gross.GrossWage - tds.MonthlyTDS - pf.EmployeeContribution
                    - esi.EmployeeContribution - pt.Amount - lwf.EmployeeAmount;

        return new PayrollResult(emp.EmployeeId, gross, tds, pf, esi, pt, lwf, net);
    }
}
```

---

## Testing the Engine

```csharp
// Always: real decimal values from statutory spec
// Never: mock calculators
// Use: Bogus for realistic employee data, hardcoded expected values for tax assertions

[Fact]
public void TDS_NewRegime_IncomeUnder700k_Rebate87A_Applied()
{
    var slabs = StatutoryTestData.NewRegimeSlabs_FY2026();
    var result = TDSCalculator.Compute(
        annualGross: 650_000m,
        priorEmployerTDSDeducted: 0m,
        priorEmployerTaxableIncome: 0m,
        slabs: slabs,
        monthsRemaining: 12);

    // ₹6,50,000 - ₹75,000 std deduction = ₹5,75,000 taxable
    // Tax: (5,75,000 - 3,00,000) * 5% = ₹13,750
    // Cess: ₹13,750 * 4% = ₹550
    // Total tax: ₹14,300 — but ≤ ₹7L after std deduction → rebate 87A wipes it
    result.AnnualTaxLiability.Should().Be(0m);
    result.MonthlyTDS.Should().Be(0m);
}
```

Each statutory scenario (rebate, surcharge, mid-year joiner, LOP month, ESI threshold crossing) must have its own test with exact expected values verified against statutory spec.
