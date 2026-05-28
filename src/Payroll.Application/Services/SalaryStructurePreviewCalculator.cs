using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using EngineCalculators = Payroll.Engine.Calculators;
using EngineInputs = Payroll.Engine.Inputs;
using EngineOutputs = Payroll.Engine.Outputs;

namespace Payroll.Application.Services;

// Shared, pure preview calculator for salary structures.
//
// Computes the per-component monthly + annual breakdown shown in the salary
// structure builder, the employee hire wizard, and the employee salary view.
//
// PR4 expanded the output to include employee-side statutory deductions
// (employee EPF, ESI, PT, LWF), net pay (take-home), and a separate benefits
// section. Statutory math delegates to the engine's calculators so preview
// always matches what payroll will actually deduct at run time. No duplicated
// statutory logic lives here — only the orchestration that prepares engine
// inputs from the template + employee context.
//
// Pure. No DI, no I/O, no async. Inputs supplied by caller (template + engine
// statutory config + employee flags + per-state PT/LWF data + benefits).
public static class SalaryStructurePreviewCalculator
{
    public sealed record Inputs(
        decimal AnnualCtc,
        IReadOnlyList<SalaryStructureComponent> TemplateComponents,
        IReadOnlyDictionary<Guid, EmployeeSalaryComponentOverride> Overrides,
        IReadOnlyList<AddedComponent> AddedComponents,
        IReadOnlyList<BenefitInput> Benefits,
        EmployeeStatutoryFlags EmployeeFlags,
        // Engine statutory config — built by StatutoryConfigBuilder from real DB
        // rows (org config + IT config + PT slabs + LWF configs). Single source
        // of statutory truth; eliminates the per-page hardcoded caps that the
        // earlier preview carried.
        EngineInputs.StatutoryConfig EngineConfig,
        // Work state for PT + LWF matching. Null when the builder previews a
        // template with no employee context — PT and LWF then return zero with
        // an "estimated" caveat for the UI to surface.
        string? WorkStateCode,
        // Calendar month + year used for state-specific frequency rules
        // (Annual / HalfYearly / HalfYearlySplit). Defaults to current month if
        // caller passes null.
        int Year,
        int Month);

    public sealed record AddedComponent(
        Guid ComponentId,
        string Code,
        string Name,
        EarningType? EarningType,
        bool ConsiderForEpf,
        bool ConsiderForEsi,
        EmployeeSalaryComponentOverride Override);

    public sealed record BenefitInput(
        Guid ComponentId,
        string Code,
        string Name,
        // Annual amount; preview divides by 12 for display only. Benefits are
        // tax-treatment-specific and the engine handles them — preview just
        // shows the operator what's in the package.
        decimal AnnualAmount);

    public sealed record EmployeeStatutoryFlags(
        bool EpfEnabled,
        bool EsiEnabled,
        bool PtEnabled,
        bool LwfEnabled,
        bool GratuityEnabled,
        bool IsPwd = false);

    public sealed record Output(
        IReadOnlyList<PreviewRow> Rows,
        IReadOnlyList<EmployerContributionRow> EmployerContributions,
        IReadOnlyList<EmployeeDeductionRow> EmployeeDeductions,
        decimal NetPayMonthly,
        IReadOnlyList<BenefitRow> Benefits);

    public sealed record PreviewRow(
        Guid ComponentId,
        string Code,
        string Name,
        ComponentFormulaType FormulaType,
        decimal? Percentage,
        decimal? FixedAmount,
        decimal MonthlyAmount,
        decimal AnnualAmount,
        bool IsResidual,
        bool IsAdded,
        bool IsOverride);

    public sealed record EmployerContributionRow(
        string Code,
        string Name,
        decimal MonthlyAmount,
        decimal AnnualAmount);

    public sealed record EmployeeDeductionRow(
        string Code,
        string Name,
        decimal MonthlyAmount,
        decimal AnnualAmount);

    public sealed record BenefitRow(
        string Code,
        string Name,
        decimal MonthlyAmount,
        decimal AnnualAmount);

    public static Output Compute(Inputs inputs)
    {
        decimal annualCtc = inputs.AnnualCtc;
        decimal monthlyGross = annualCtc / 12m;

        List<PreviewRow> rows = [];
        decimal basicMonthly = 0m;
        decimal nonResidualMonthly = 0m;
        decimal pfWageMonthly = 0m;
        decimal fullPfWageMonthly = 0m;
        decimal esiWageMonthly = 0m;
        SalaryStructureComponent? residual = null;

        IOrderedEnumerable<SalaryStructureComponent> ordered =
            inputs.TemplateComponents.OrderBy(c => c.DisplayOrder);

        foreach (SalaryStructureComponent comp in ordered)
        {
            if (comp.Component is null) continue;
            if (comp.FormulaType == ComponentFormulaType.ResidualCTC)
            {
                residual = comp;
                continue;
            }

            bool hasOverride = inputs.Overrides.TryGetValue(comp.ComponentId, out EmployeeSalaryComponentOverride? ov);
            ComponentFormulaType effectiveType = hasOverride ? ov!.FormulaType : comp.FormulaType;
            decimal? effectivePct = hasOverride ? ov!.Percentage : comp.Percentage;
            decimal? effectiveFixed = hasOverride ? ov!.FixedAmount : comp.FixedAmount;

            decimal monthly = EvaluateMonthly(effectiveType, effectivePct, effectiveFixed, annualCtc, basicMonthly, monthlyGross);

            if (comp.Component.EarningType == EarningType.Basic)
                basicMonthly = monthly;

            nonResidualMonthly += monthly;

            if (comp.Component.ConsiderForEpf == true)
            {
                pfWageMonthly += monthly;
                fullPfWageMonthly += monthly;
            }
            if (comp.Component.ConsiderForEsi == true)
                esiWageMonthly += monthly;

            rows.Add(new PreviewRow(
                comp.ComponentId, comp.Component.Code, comp.Component.Name,
                effectiveType, effectivePct, effectiveFixed,
                monthly, RoundUp(monthly * 12m),
                IsResidual: false, IsAdded: false, IsOverride: hasOverride));
        }

        // Pass 2: added components (override-only, not part of template).
        foreach (AddedComponent added in inputs.AddedComponents)
        {
            EmployeeSalaryComponentOverride ov = added.Override;
            decimal monthly = EvaluateMonthly(ov.FormulaType, ov.Percentage, ov.FixedAmount, annualCtc, basicMonthly, monthlyGross);
            nonResidualMonthly += monthly;
            if (added.ConsiderForEpf)
            {
                pfWageMonthly += monthly;
                fullPfWageMonthly += monthly;
            }
            if (added.ConsiderForEsi)
                esiWageMonthly += monthly;

            rows.Add(new PreviewRow(
                added.ComponentId, added.Code, added.Name,
                ov.FormulaType, ov.Percentage, ov.FixedAmount,
                monthly, RoundUp(monthly * 12m),
                IsResidual: false, IsAdded: true, IsOverride: true));
        }

        // ── Employer statutory load (in CTC) ─────────────────────────────────
        EngineInputs.StatutoryConfig cfg = inputs.EngineConfig;
        decimal employerEpfMonthly = 0m;
        decimal gratuityMonthly = 0m;

        if (inputs.EmployeeFlags.EpfEnabled
            && cfg.PFEnabled
            && cfg.EpfIncludeEmployerInCtc
            && pfWageMonthly > 0m)
        {
            decimal cappedPfWage = cfg.EpfRestrictEmployerWage
                ? Math.Min(pfWageMonthly, cfg.PFWageCap)
                : pfWageMonthly;
            employerEpfMonthly = RoundUp(cappedPfWage * cfg.EPFEmployeeRate);
        }

        if (inputs.EmployeeFlags.GratuityEnabled
            && cfg.GratuityIncludedInCtc
            && basicMonthly > 0m)
        {
            gratuityMonthly = RoundUp(basicMonthly * 15m / 26m / 12m);
        }

        decimal employerStatutoryMonthly = employerEpfMonthly + gratuityMonthly;

        // ── Residual (Special Allowance) ─────────────────────────────────────
        if (residual?.Component is not null)
        {
            decimal residualMonthly = Math.Max(0m, monthlyGross - nonResidualMonthly - employerStatutoryMonthly);
            rows.Add(new PreviewRow(
                residual.ComponentId, residual.Component.Code, residual.Component.Name,
                ComponentFormulaType.ResidualCTC, null, null,
                RoundUp(residualMonthly), RoundUp(residualMonthly * 12m),
                IsResidual: true, IsAdded: false, IsOverride: false));
        }

        List<EmployerContributionRow> employerRows = [];
        if (employerEpfMonthly > 0m)
            employerRows.Add(new EmployerContributionRow(
                "EPF_EMPLOYER", "Employer EPF + EPS",
                employerEpfMonthly, RoundUp(employerEpfMonthly * 12m)));
        if (gratuityMonthly > 0m)
            employerRows.Add(new EmployerContributionRow(
                "GRATUITY_ACCRUAL", "Gratuity accrual",
                gratuityMonthly, RoundUp(gratuityMonthly * 12m)));

        // ── Employee deductions ──────────────────────────────────────────────
        List<EmployeeDeductionRow> employeeDeductions = ComputeEmployeeDeductions(
            inputs, cfg, pfWageMonthly, fullPfWageMonthly, esiWageMonthly, monthlyGross);

        decimal totalDeductions = 0m;
        foreach (EmployeeDeductionRow d in employeeDeductions)
            totalDeductions += d.MonthlyAmount;
        decimal netPayMonthly = Math.Max(0m, monthlyGross - totalDeductions);

        // ── Benefits ─────────────────────────────────────────────────────────
        List<BenefitRow> benefitRows = inputs.Benefits
            .Select(b => new BenefitRow(
                b.Code, b.Name,
                RoundUp(b.AnnualAmount / 12m),
                RoundUp(b.AnnualAmount)))
            .ToList();

        return new Output(rows, employerRows, employeeDeductions, RoundUp(netPayMonthly), benefitRows);
    }

    private static List<EmployeeDeductionRow> ComputeEmployeeDeductions(
        Inputs inputs,
        EngineInputs.StatutoryConfig cfg,
        decimal pfWageMonthly,
        decimal fullPfWageMonthly,
        decimal esiWageMonthly,
        decimal grossMonthly)
    {
        List<EmployeeDeductionRow> deductions = [];

        // Employee EPF — delegate to engine PFCalculator. baseDays=30 + lopDays=0
        // means preview shows the standard-month figure (no LOP, no proration);
        // run-time engine handles per-period proration.
        EngineOutputs.PFResult pf = EngineCalculators.PFCalculator.Compute(
            pfWage: pfWageMonthly,
            fullPfWage: fullPfWageMonthly,
            lopDays: 0m,
            baseDays: 30,
            config: cfg,
            optOut: !inputs.EmployeeFlags.EpfEnabled);
        if (pf.EmployeeContribution > 0m)
            deductions.Add(new EmployeeDeductionRow(
                "EPF_EMPLOYEE", "Employee EPF",
                pf.EmployeeContribution, RoundUp(pf.EmployeeContribution * 12m)));

        // Employee ESI — delegate to engine ESICalculator.
        EngineOutputs.ESIResult esi = EngineCalculators.ESICalculator.Compute(
            grossWage: esiWageMonthly,
            config: cfg,
            isExempt: !inputs.EmployeeFlags.EsiEnabled,
            isPWD: inputs.EmployeeFlags.IsPwd);
        if (esi.EmployeeContribution > 0m)
            deductions.Add(new EmployeeDeductionRow(
                "ESI_EMPLOYEE", "Employee ESI",
                esi.EmployeeContribution, RoundUp(esi.EmployeeContribution * 12m)));

        // PT — only when work state is known (wizard + employee tab). Builder
        // previews template-only and passes WorkStateCode=null; we skip PT
        // there since slabs are state-keyed.
        if (inputs.EmployeeFlags.PtEnabled && !string.IsNullOrWhiteSpace(inputs.WorkStateCode))
        {
            EngineInputs.EmployeeInput ptEmp = MinimalEmployeeInputForStateLookup(
                inputs.WorkStateCode!, inputs.EmployeeFlags.IsPwd);
            EngineInputs.PayrollRunInput run = MinimalRunInput(inputs.Year, inputs.Month);
            EngineOutputs.PTResult pt = EngineCalculators.PTCalculator.Compute(grossMonthly, ptEmp, cfg, run);
            if (pt.Amount > 0m)
                deductions.Add(new EmployeeDeductionRow(
                    "PT_EMPLOYEE", $"Professional Tax ({inputs.WorkStateCode})",
                    pt.Amount, RoundUp(pt.Amount * 12m)));
        }

        // LWF — same state-gated treatment as PT.
        if (inputs.EmployeeFlags.LwfEnabled && !string.IsNullOrWhiteSpace(inputs.WorkStateCode))
        {
            EngineInputs.PayrollRunInput run = MinimalRunInput(inputs.Year, inputs.Month);
            EngineOutputs.LWFResult lwf = EngineCalculators.LWFCalculator.Compute(
                inputs.WorkStateCode!, grossMonthly, cfg, run);
            if (lwf.EmployeeAmount > 0m)
                deductions.Add(new EmployeeDeductionRow(
                    "LWF_EMPLOYEE", $"Employee LWF ({inputs.WorkStateCode})",
                    lwf.EmployeeAmount, RoundUp(lwf.EmployeeAmount * 12m)));
        }

        return deductions;
    }

    private static EngineInputs.EmployeeInput MinimalEmployeeInputForStateLookup(string stateCode, bool isPwd) =>
        new(
            EmployeeId: Guid.Empty, EmployeeCode: "PREVIEW", WorkStateCode: stateCode,
            EpfEnabled: true, IsESIExempt: false, IsPWD: isPwd, MonthlyCTC: 0m,
            Components: [], LOPDays: 0, WorkingDaysInMonth: 30, VPFAmount: 0,
            PriorEmployerYTDTaxableIncome: 0, PriorEmployerYTDTDSDeducted: 0, PriorEmployerYTDPF: 0,
            HalfYearMonthIndex: 1, HalfYearTotalMonths: 6);

    private static EngineInputs.PayrollRunInput MinimalRunInput(int year, int month) =>
        new(Year: year, Month: month, CalendarDaysInMonth: 30, SalaryDivisor: 30,
            MonthsRemainingInFY: 12, FiscalYearLabel: $"FY{(month >= 4 ? year + 1 : year)}");

    private static decimal RoundUp(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal EvaluateMonthly(
        ComponentFormulaType type, decimal? pct, decimal? fixedAmount,
        decimal annualCtc, decimal basicMonthly, decimal monthlyGross) =>
        type switch
        {
            ComponentFormulaType.PercentOfCTC =>
                pct.HasValue ? RoundUp(annualCtc * (pct.Value / 100m) / 12m) : 0m,
            ComponentFormulaType.PercentOfBasic =>
                pct.HasValue ? RoundUp(basicMonthly * (pct.Value / 100m)) : 0m,
            ComponentFormulaType.PercentOfGross =>
                pct.HasValue ? RoundUp(monthlyGross * (pct.Value / 100m)) : 0m,
            ComponentFormulaType.Fixed => fixedAmount ?? 0m,
            _ => 0m,
        };
}
