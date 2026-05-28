using MediatR;
using Payroll.Application.Services;
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
    IReadOnlyList<SalaryComponentBreakdownDto> Components,
    IReadOnlyList<EmployerContributionDto> EmployerContributions);

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

public sealed record EmployerContributionDto(
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed class GetEmployeeSalaryStructureHandler(
    IEmployeeSalaryStructureRepository salaryRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository componentRepo,
    IEmployeeRepository employeeRepo,
    IStatutoryConfigRepository statutoryRepo)
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

        decimal monthlyGross = structure.AnnualCTC / 12m;

        // Pull employee statutory flags + org config so the preview matches what
        // the engine will actually produce at run time. Falls back to a permissive
        // "all on" stance if either is missing (legacy data) — that preserves the
        // pre-fix behaviour rather than mis-stating the residual the other way.
        Employee? employee = await employeeRepo.GetByIdAsync(request.EmployeeId, ct);
        StatutoryOrgConfig? orgConfig = await statutoryRepo.GetByTenantAsync(ct);

        SalaryStructurePreviewCalculator.EmployeeStatutoryFlags employeeFlags = new(
            EpfEnabled: employee?.EpfEnabled ?? true,
            EsiEnabled: employee?.EsiEnabled ?? true,
            PtEnabled: employee?.PtEnabled ?? true,
            LwfEnabled: employee?.LwfEnabled ?? true,
            GratuityEnabled: true);

        StatutoryOrgConfig effectiveOrgConfig = orgConfig
            ?? StatutoryOrgConfig.CreateDefault(Guid.Empty, Guid.Empty);

        // Identify added (override-only, not in template) components so we can load
        // their EarningType + ConsiderForEpf flags (needed for residual + employer
        // statutory math). Bulk load in one round-trip.
        HashSet<Guid> templateComponentIds = template?.Components
            .Select(c => c.ComponentId).ToHashSet() ?? [];
        List<Guid> addedComponentIds = overrideMap.Keys
            .Where(id => !templateComponentIds.Contains(id))
            .ToList();
        Dictionary<Guid, SalaryComponent> addedComponentMap = addedComponentIds.Count > 0
            ? (await componentRepo.GetByIdsAsync(addedComponentIds, ct)).ToDictionary(c => c.Id)
            : [];

        List<SalaryStructurePreviewCalculator.AddedComponent> addedForCalc = [];
        foreach (EmployeeSalaryComponentOverride ov in structure.ComponentOverrides
            .Where(o => addedComponentIds.Contains(o.SalaryComponentId)))
        {
            if (!addedComponentMap.TryGetValue(ov.SalaryComponentId, out SalaryComponent? sc)) continue;
            addedForCalc.Add(new SalaryStructurePreviewCalculator.AddedComponent(
                ov.SalaryComponentId, sc.Code, sc.Name, sc.EarningType,
                ConsiderForEpf: sc.ConsiderForEpf == true,
                Override: ov));
        }

        SalaryStructurePreviewCalculator.Output preview = SalaryStructurePreviewCalculator.Compute(
            new SalaryStructurePreviewCalculator.Inputs(
                AnnualCtc: structure.AnnualCTC,
                TemplateComponents: template?.Components ?? [],
                Overrides: overrideMap,
                AddedComponents: addedForCalc,
                EmployeeFlags: employeeFlags,
                OrgConfig: effectiveOrgConfig,
                Caps: new SalaryStructurePreviewCalculator.StatutoryCaps()));

        List<SalaryComponentBreakdownDto> components = preview.Rows
            .Select(r => new SalaryComponentBreakdownDto(
                r.ComponentId, r.Name, r.Code,
                r.FormulaType.ToString(), r.Percentage,
                r.MonthlyAmount, r.AnnualAmount, r.IsResidual, r.IsOverride || r.IsAdded))
            .ToList();

        List<EmployerContributionDto> employerContributions = preview.EmployerContributions
            .Select(e => new EmployerContributionDto(e.Code, e.Name, e.MonthlyAmount, e.AnnualAmount))
            .ToList();

        return new EmployeeSalaryStructureDto(
            structure.Id,
            structure.AnnualCTC,
            monthlyGross,
            template?.Id,
            template?.Name,
            structure.EffectiveFrom,
            components,
            employerContributions);
    }
}
