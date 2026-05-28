using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Application.Services;

// Shared, pure preview calculator for salary structures.
//
// Computes the per-component monthly + annual breakdown shown in the salary structure
// builder, the employee hire wizard, and the employee salary view. Until this lived in
// one place, three independent implementations existed (settings builder TS, wizard TS,
// query handler C#), each computing the residual (Special / Fixed Allowance) as
// `CTC/12 − Σ earnings` without subtracting the employer-side statutory load that the
// org's StatutoryOrgConfig says is part of CTC. That over-stated the residual by the
// employer-EPF + employer-EPS + gratuity accrual, sometimes by ₹50k+/year, and the
// number drifted from what the engine actually pays out at run time.
//
// This is pure. No DI, no I/O, no async. Inputs come from the caller (template + org
// config + employee flags + caps) and the returned breakdown is the single source of
// truth for the UI. The engine itself remains the source of truth for paid runs; this
// calculator only mirrors the engine's CTC composition math for preview purposes.
public static class SalaryStructurePreviewCalculator
{
    public sealed record Inputs(
        decimal AnnualCtc,
        IReadOnlyList<SalaryStructureComponent> TemplateComponents,
        IReadOnlyDictionary<Guid, EmployeeSalaryComponentOverride> Overrides,
        IReadOnlyList<AddedComponent> AddedComponents,
        EmployeeStatutoryFlags EmployeeFlags,
        StatutoryOrgConfig OrgConfig,
        StatutoryCaps Caps);

    public sealed record AddedComponent(
        Guid ComponentId,
        string Code,
        string Name,
        EarningType? EarningType,
        bool ConsiderForEpf,
        EmployeeSalaryComponentOverride Override);

    // Per-employee enable flags. Mirrors the booleans on Employee + the gratuity flag
    // (derived from the employee's gratuity opt-in, default true).
    public sealed record EmployeeStatutoryFlags(
        bool EpfEnabled,
        bool EsiEnabled,
        bool PtEnabled,
        bool LwfEnabled,
        bool GratuityEnabled);

    // Caps from IncomeTaxConfig that the calculator needs. Defaults below match the
    // FY 2025-26 statutory values so callers without an active IncomeTaxConfig still
    // get an accurate preview.
    public sealed record StatutoryCaps(
        decimal PfWageCap = 15_000m,
        decimal EpfEmployerRate = 0.12m);

    public sealed record Output(
        IReadOnlyList<PreviewRow> Rows,
        IReadOnlyList<EmployerContributionRow> EmployerContributions);

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

    public static Output Compute(Inputs inputs)
    {
        decimal annualCtc = inputs.AnnualCtc;
        decimal monthlyGross = annualCtc / 12m;

        List<PreviewRow> rows = [];
        decimal basicMonthly = 0m;
        decimal nonResidualMonthly = 0m;
        decimal pfWageMonthly = 0m;
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
                pfWageMonthly += monthly;

            rows.Add(new PreviewRow(
                comp.ComponentId,
                comp.Component.Code,
                comp.Component.Name,
                effectiveType,
                effectivePct,
                effectiveFixed,
                monthly,
                Math.Round(monthly * 12m, 2),
                IsResidual: false,
                IsAdded: false,
                IsOverride: hasOverride));
        }

        // Pass 2: added components (override-only, not part of template).
        foreach (AddedComponent added in inputs.AddedComponents)
        {
            EmployeeSalaryComponentOverride ov = added.Override;
            decimal monthly = EvaluateMonthly(ov.FormulaType, ov.Percentage, ov.FixedAmount, annualCtc, basicMonthly, monthlyGross);
            nonResidualMonthly += monthly;
            if (added.ConsiderForEpf)
                pfWageMonthly += monthly;

            rows.Add(new PreviewRow(
                added.ComponentId,
                added.Code,
                added.Name,
                ov.FormulaType,
                ov.Percentage,
                ov.FixedAmount,
                monthly,
                Math.Round(monthly * 12m, 2),
                IsResidual: false,
                IsAdded: true,
                IsOverride: true));
        }

        // Employer statutory load — only the parts the org chooses to include in CTC
        // AND the employee actually has enabled at the flag level. Both gates must agree.
        decimal employerEpfMonthly = 0m;
        decimal gratuityMonthly = 0m;

        if (inputs.EmployeeFlags.EpfEnabled
            && inputs.OrgConfig.EpfEnabled
            && inputs.OrgConfig.EpfIncludeEmployerInCtc
            && pfWageMonthly > 0m)
        {
            decimal cappedPfWage = Math.Min(pfWageMonthly, inputs.Caps.PfWageCap);
            employerEpfMonthly = Math.Round(cappedPfWage * inputs.Caps.EpfEmployerRate, 2);
        }

        if (inputs.EmployeeFlags.GratuityEnabled
            && inputs.OrgConfig.GratuityIncludedInCtc
            && basicMonthly > 0m)
        {
            // Gratuity accrual = 15/26 of basic per year → monthly = basic × 15 / 26 / 12.
            // Matches Payroll.Engine.GratuityCalculator exactly.
            gratuityMonthly = Math.Round(basicMonthly * 15m / 26m / 12m, 2);
        }

        decimal employerStatutoryMonthly = employerEpfMonthly + gratuityMonthly;

        // Residual = monthlyGross − non-residual earnings − employer statutory in CTC.
        // Clamped at zero: if structure + employer statutory already exceeds CTC, the
        // operator has over-allocated and should fix the template, not see a negative row.
        if (residual?.Component is not null)
        {
            decimal residualMonthly = Math.Max(0m, monthlyGross - nonResidualMonthly - employerStatutoryMonthly);
            rows.Add(new PreviewRow(
                residual.ComponentId,
                residual.Component.Code,
                residual.Component.Name,
                ComponentFormulaType.ResidualCTC,
                null,
                null,
                Math.Round(residualMonthly, 2),
                Math.Round(residualMonthly * 12m, 2),
                IsResidual: true,
                IsAdded: false,
                IsOverride: false));
        }

        List<EmployerContributionRow> employerRows = [];
        if (employerEpfMonthly > 0m)
            employerRows.Add(new EmployerContributionRow(
                "EPF_EMPLOYER", "Employer EPF + EPS",
                employerEpfMonthly, Math.Round(employerEpfMonthly * 12m, 2)));
        if (gratuityMonthly > 0m)
            employerRows.Add(new EmployerContributionRow(
                "GRATUITY_ACCRUAL", "Gratuity accrual",
                gratuityMonthly, Math.Round(gratuityMonthly * 12m, 2)));

        return new Output(rows, employerRows);
    }

    private static decimal EvaluateMonthly(
        ComponentFormulaType type, decimal? pct, decimal? fixedAmount,
        decimal annualCtc, decimal basicMonthly, decimal monthlyGross) =>
        type switch
        {
            ComponentFormulaType.PercentOfCTC =>
                pct.HasValue ? Math.Round(annualCtc * (pct.Value / 100m) / 12m, 2) : 0m,
            ComponentFormulaType.PercentOfBasic =>
                pct.HasValue ? Math.Round(basicMonthly * (pct.Value / 100m), 2) : 0m,
            ComponentFormulaType.PercentOfGross =>
                pct.HasValue ? Math.Round(monthlyGross * (pct.Value / 100m), 2) : 0m,
            ComponentFormulaType.Fixed => fixedAmount ?? 0m,
            _ => 0m,
        };
}
