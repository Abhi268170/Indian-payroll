using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeSalaryStructureQuery(Guid EmployeeId) : IRequest<EmployeeSalaryStructureDto?>;

public sealed record EmployeeSalaryStructureDto(
    Guid Id,
    decimal AnnualCTC,
    decimal MonthlyGross,
    Guid? TemplateId,
    string? TemplateName,
    DateOnly EffectiveFrom,
    IReadOnlyList<SalaryComponentBreakdownDto> Components);

public sealed record SalaryComponentBreakdownDto(
    Guid ComponentId,
    string ComponentName,
    string ComponentCode,
    string FormulaType,
    decimal? Percentage,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    bool IsResidual,
    bool IsOverride);

public sealed class GetEmployeeSalaryStructureHandler(
    IEmployeeSalaryStructureRepository salaryRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository componentRepo)
    : IRequestHandler<GetEmployeeSalaryStructureQuery, EmployeeSalaryStructureDto?>
{
    public async Task<EmployeeSalaryStructureDto?> Handle(
        GetEmployeeSalaryStructureQuery request, CancellationToken ct)
    {
        EmployeeSalaryStructure? structure = await salaryRepo.GetActiveWithOverridesAsync(request.EmployeeId, ct);
        if (structure is null) return null;

        SalaryStructureTemplate? template = structure.SalaryStructureTemplateId.HasValue
            ? await templateRepo.GetByIdWithComponentsAsync(structure.SalaryStructureTemplateId.Value, ct)
            : null;

        Dictionary<Guid, EmployeeSalaryComponentOverride> overrideMap =
            structure.ComponentOverrides.ToDictionary(o => o.SalaryComponentId);

        List<SalaryComponentBreakdownDto> components = [];
        decimal monthlyGross = structure.AnnualCTC / 12m;

        HashSet<Guid> templateComponentIds = [];

        if (template is not null)
        {
            decimal basicMonthly = 0m;
            decimal nonResidualSum = 0m;

            IOrderedEnumerable<SalaryStructureComponent> ordered =
                template.Components.OrderBy(c => c.DisplayOrder);

            foreach (SalaryStructureComponent comp in ordered)
            {
                if (comp.Component is null) continue;
                templateComponentIds.Add(comp.ComponentId);
                if (comp.FormulaType == ComponentFormulaType.ResidualCTC) continue;

                bool hasOverride = overrideMap.TryGetValue(comp.ComponentId, out EmployeeSalaryComponentOverride? ov);
                ComponentFormulaType effectiveType = hasOverride ? ov!.FormulaType : comp.FormulaType;
                decimal? effectivePct = hasOverride ? ov!.Percentage : comp.Percentage;
                decimal? effectiveFixed = hasOverride ? ov!.FixedAmount : comp.FixedAmount;

                decimal monthly = effectiveType switch
                {
                    ComponentFormulaType.PercentOfCTC =>
                        Math.Round(structure.AnnualCTC * (effectivePct!.Value / 100m) / 12m, 2),
                    ComponentFormulaType.PercentOfBasic =>
                        Math.Round(basicMonthly * (effectivePct!.Value / 100m), 2),
                    ComponentFormulaType.PercentOfGross =>
                        Math.Round(monthlyGross * (effectivePct!.Value / 100m), 2),
                    ComponentFormulaType.Fixed =>
                        effectiveFixed ?? 0m,
                    _ => 0m,
                };

                if (comp.Component.Code == "BASIC")
                    basicMonthly = monthly;

                nonResidualSum += monthly;

                components.Add(new SalaryComponentBreakdownDto(
                    comp.ComponentId,
                    comp.Component.Name,
                    comp.Component.Code,
                    effectiveType.ToString(),
                    effectivePct,
                    monthly,
                    Math.Round(monthly * 12m, 2),
                    false,
                    hasOverride));
            }

            // Residual component (Fixed Allowance)
            SalaryStructureComponent? residual = template.Components
                .FirstOrDefault(c => c.FormulaType == ComponentFormulaType.ResidualCTC);
            if (residual?.Component is not null)
            {
                templateComponentIds.Add(residual.ComponentId);
                decimal residualMonthly = Math.Round(monthlyGross - nonResidualSum, 2);
                components.Add(new SalaryComponentBreakdownDto(
                    residual.ComponentId,
                    residual.Component.Name,
                    residual.Component.Code,
                    residual.FormulaType.ToString(),
                    null,
                    residualMonthly,
                    Math.Round(residualMonthly * 12m, 2),
                    true,
                    false));
            }
        }

        // Pass 2: override-only (added earnings not in template)
        List<Guid> addedIds = overrideMap.Keys.Where(id => !templateComponentIds.Contains(id)).ToList();
        if (addedIds.Count > 0)
        {
            List<SalaryComponent> addedComponents = await componentRepo.GetByIdsAsync(addedIds, ct);
            Dictionary<Guid, SalaryComponent> addedMap = addedComponents.ToDictionary(c => c.Id);
            foreach (EmployeeSalaryComponentOverride ov in structure.ComponentOverrides
                .Where(o => addedIds.Contains(o.SalaryComponentId)))
            {
                if (!addedMap.TryGetValue(ov.SalaryComponentId, out SalaryComponent? sc)) continue;

                decimal monthly = ov.FormulaType switch
                {
                    ComponentFormulaType.Fixed => ov.FixedAmount ?? 0m,
                    _ => 0m,
                };

                components.Add(new SalaryComponentBreakdownDto(
                    ov.SalaryComponentId,
                    sc.Name,
                    sc.Code,
                    ov.FormulaType.ToString(),
                    ov.Percentage,
                    monthly,
                    Math.Round(monthly * 12m, 2),
                    false,
                    true));
            }
        }

        return new EmployeeSalaryStructureDto(
            structure.Id,
            structure.AnnualCTC,
            monthlyGross,
            template?.Id,
            template?.Name,
            structure.EffectiveFrom,
            components);
    }
}
