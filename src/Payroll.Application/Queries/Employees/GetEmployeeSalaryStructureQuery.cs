using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Extensions;
using Payroll.Domain.Interfaces;
using EngineInputs = Payroll.Engine.Inputs;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeSalaryStructureQuery(Guid EmployeeId) : IRequest<EmployeeSalaryStructureDto?>;

public sealed record EmployeeSalaryStructureDto(
    Guid Id,
    decimal AnnualCTC,
    decimal MonthlyGross,
    decimal NetPayMonthly,
    Guid? TemplateId,
    string? TemplateName,
    DateOnly EffectiveFrom,
    IReadOnlyList<SalaryComponentBreakdownDto> Components,
    IReadOnlyList<EmployerContributionDto> EmployerContributions,
    IReadOnlyList<EmployeeDeductionDto> EmployeeDeductions,
    IReadOnlyList<BenefitDto> Benefits);

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

public sealed record EmployeeDeductionDto(
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed record BenefitDto(
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed class GetEmployeeSalaryStructureHandler(
    IEmployeeSalaryStructureRepository salaryRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ISalaryComponentRepository componentRepo,
    IEmployeeRepository employeeRepo,
    IWorkLocationRepository workLocationRepo,
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

        Employee? employee = await employeeRepo.GetByIdAsync(request.EmployeeId, ct);

        // Resolve state from employee's work location so PT + LWF preview match
        // engine deductions for the actual payroll run.
        string? workStateCode = null;
        if (employee is not null)
        {
            WorkLocation? wl = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
            workStateCode = wl?.State.ToIsoCode();
        }

        SalaryStructurePreviewCalculator.EmployeeStatutoryFlags employeeFlags = new(
            EpfEnabled: employee?.EpfEnabled ?? true,
            EsiEnabled: employee?.EsiEnabled ?? true,
            PtEnabled: employee?.PtEnabled ?? true,
            LwfEnabled: employee?.LwfEnabled ?? true,
            GratuityEnabled: true,
            IsPwd: employee?.IsPWD ?? false);

        // Identify added (override-only, not in template) components.
        HashSet<Guid> templateComponentIds = template?.Components
            .Select(c => c.ComponentId).ToHashSet() ?? [];
        List<Guid> addedComponentIds = overrideMap.Keys
            .Where(id => !templateComponentIds.Contains(id))
            .ToList();
        Dictionary<Guid, SalaryComponent> addedComponentMap = addedComponentIds.Count > 0
            ? (await componentRepo.GetByIdsAsync(addedComponentIds, ct)).ToDictionary(c => c.Id)
            : [];

        List<SalaryStructurePreviewCalculator.AddedComponent> addedForCalc = [];
        List<SalaryStructurePreviewCalculator.BenefitInput> benefitsForCalc = [];
        foreach (EmployeeSalaryComponentOverride ov in structure.ComponentOverrides
            .Where(o => addedComponentIds.Contains(o.SalaryComponentId)))
        {
            if (!addedComponentMap.TryGetValue(ov.SalaryComponentId, out SalaryComponent? sc)) continue;

            // Benefit-category components are surfaced separately. Engine
            // handles benefit taxability — preview only displays.
            if (sc.Category == ComponentCategory.Benefit)
            {
                decimal annual = (ov.FormulaType == ComponentFormulaType.Fixed && ov.FixedAmount.HasValue)
                    ? ov.FixedAmount.Value * 12m
                    : 0m;
                benefitsForCalc.Add(new SalaryStructurePreviewCalculator.BenefitInput(
                    ov.SalaryComponentId, sc.Code, sc.Name, annual));
                continue;
            }

            addedForCalc.Add(new SalaryStructurePreviewCalculator.AddedComponent(
                ov.SalaryComponentId, sc.Code, sc.Name, sc.EarningType,
                ConsiderForEpf: sc.ConsiderForEpf == true,
                ConsiderForEsi: sc.ConsiderForEsi == true,
                Override: ov));
        }

        EngineInputs.StatutoryConfig engineConfig = await BuildEngineConfigAsync(workStateCode, ct);

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        SalaryStructurePreviewCalculator.Output preview = SalaryStructurePreviewCalculator.Compute(
            new SalaryStructurePreviewCalculator.Inputs(
                AnnualCtc: structure.AnnualCTC,
                TemplateComponents: template?.Components ?? [],
                Overrides: overrideMap,
                AddedComponents: addedForCalc,
                Benefits: benefitsForCalc,
                EmployeeFlags: employeeFlags,
                EngineConfig: engineConfig,
                WorkStateCode: workStateCode,
                Year: today.Year,
                Month: today.Month));

        List<SalaryComponentBreakdownDto> components = preview.Rows
            .Select(r => new SalaryComponentBreakdownDto(
                r.ComponentId, r.Name, r.Code,
                r.FormulaType.ToString(), r.Percentage,
                r.MonthlyAmount, r.AnnualAmount, r.IsResidual, r.IsOverride || r.IsAdded))
            .ToList();

        List<EmployerContributionDto> employerContributions = preview.EmployerContributions
            .Select(e => new EmployerContributionDto(e.Code, e.Name, e.MonthlyAmount, e.AnnualAmount))
            .ToList();

        List<EmployeeDeductionDto> employeeDeductions = preview.EmployeeDeductions
            .Select(d => new EmployeeDeductionDto(d.Code, d.Name, d.MonthlyAmount, d.AnnualAmount))
            .ToList();

        List<BenefitDto> benefits = preview.Benefits
            .Select(b => new BenefitDto(b.Code, b.Name, b.MonthlyAmount, b.AnnualAmount))
            .ToList();

        return new EmployeeSalaryStructureDto(
            structure.Id,
            structure.AnnualCTC,
            monthlyGross,
            preview.NetPayMonthly,
            template?.Id,
            template?.Name,
            structure.EffectiveFrom,
            components,
            employerContributions,
            employeeDeductions,
            benefits);
    }

    private async Task<EngineInputs.StatutoryConfig> BuildEngineConfigAsync(
        string? workStateCode, CancellationToken ct)
    {
        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? StatutoryOrgConfig.CreateDefault(Guid.Empty, Guid.Empty);

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int fiscalYear = today.Month >= 4 ? today.Year : today.Year - 1;
        string fyKey = (fiscalYear + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        IncomeTaxConfig? taxConfig = await statutoryRepo.GetIncomeTaxConfigAsync(fyKey, "New", ct);
        IReadOnlyList<IncomeTaxSlab> taxSlabs = await statutoryRepo.GetIncomeTaxSlabsAsync(fyKey, "New", ct);
        IReadOnlyList<IncomeTaxSurchargeSlab> surchargeSlabs = await statutoryRepo.GetSurchargeSlabsAsync(fyKey, "New", ct);

        IReadOnlyList<ProfessionalTaxSlab> ptSlabs = !string.IsNullOrWhiteSpace(workStateCode)
            ? await statutoryRepo.GetPtSlabsAsync(workStateCode, today, ct)
            : [];

        IReadOnlyList<LwfStateConfig> lwfConfigs = !string.IsNullOrWhiteSpace(workStateCode)
            ? await statutoryRepo.GetLwfConfigsAsync([workStateCode], ct)
            : [];

        return StatutoryConfigBuilder.Build(
            orgConfig, taxConfig, taxSlabs, surchargeSlabs, ptSlabs, lwfConfigs);
    }
}
