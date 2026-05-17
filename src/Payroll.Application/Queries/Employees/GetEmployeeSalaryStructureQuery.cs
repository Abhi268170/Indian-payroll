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
    bool IsResidual);

public sealed class GetEmployeeSalaryStructureHandler(
    IEmployeeSalaryStructureRepository salaryRepo,
    ISalaryStructureTemplateRepository templateRepo)
    : IRequestHandler<GetEmployeeSalaryStructureQuery, EmployeeSalaryStructureDto?>
{
    public async Task<EmployeeSalaryStructureDto?> Handle(
        GetEmployeeSalaryStructureQuery request, CancellationToken ct)
    {
        EmployeeSalaryStructure? structure = await salaryRepo.GetActiveAsync(request.EmployeeId, ct);
        if (structure is null) return null;

        SalaryStructureTemplate? template = structure.SalaryStructureTemplateId.HasValue
            ? await templateRepo.GetByIdWithComponentsAsync(structure.SalaryStructureTemplateId.Value, ct)
            : null;

        List<SalaryComponentBreakdownDto> components = [];
        decimal monthlyGross = structure.AnnualCTC / 12m;

        if (template is not null)
        {
            decimal basicMonthly = 0m;
            decimal nonResidualSum = 0m;

            IOrderedEnumerable<SalaryStructureComponent> ordered =
                template.Components.OrderBy(c => c.DisplayOrder);

            foreach (SalaryStructureComponent comp in ordered)
            {
                if (comp.Component is null) continue;
                if (comp.FormulaType == ComponentFormulaType.ResidualCTC) continue;

                decimal monthly = comp.FormulaType switch
                {
                    ComponentFormulaType.PercentOfCTC =>
                        Math.Round(structure.AnnualCTC * (comp.Percentage!.Value / 100m) / 12m, 2),
                    ComponentFormulaType.PercentOfBasic =>
                        Math.Round(basicMonthly * (comp.Percentage!.Value / 100m), 2),
                    ComponentFormulaType.PercentOfGross =>
                        Math.Round(monthlyGross * (comp.Percentage!.Value / 100m), 2),
                    ComponentFormulaType.Fixed =>
                        comp.FixedAmount ?? 0m,
                    _ => 0m,
                };

                if (comp.Component.Code == "BASIC")
                    basicMonthly = monthly;

                nonResidualSum += monthly;

                components.Add(new SalaryComponentBreakdownDto(
                    comp.ComponentId,
                    comp.Component.Name,
                    comp.Component.Code,
                    comp.FormulaType.ToString(),
                    comp.Percentage,
                    monthly,
                    Math.Round(monthly * 12m, 2),
                    false));
            }

            // Residual component (Fixed Allowance)
            SalaryStructureComponent? residual = template.Components
                .FirstOrDefault(c => c.FormulaType == ComponentFormulaType.ResidualCTC);
            if (residual?.Component is not null)
            {
                decimal residualMonthly = Math.Round(monthlyGross - nonResidualSum, 2);
                components.Add(new SalaryComponentBreakdownDto(
                    residual.ComponentId,
                    residual.Component.Name,
                    residual.Component.Code,
                    residual.FormulaType.ToString(),
                    null,
                    residualMonthly,
                    Math.Round(residualMonthly * 12m, 2),
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
