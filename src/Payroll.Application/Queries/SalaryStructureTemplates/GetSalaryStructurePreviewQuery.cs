using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using EngineInputs = Payroll.Engine.Inputs;

namespace Payroll.Application.Queries.SalaryStructureTemplates;

// Backend-authoritative preview of a salary structure.
//
// Accepts inline template components so the settings builder can preview
// unsaved drafts. For the wizard, the caller passes the same template
// components it already has loaded plus any added/override entries.
//
// Returns the row breakdown + employer-contribution lines (in CTC) +
// employee-side deductions + net pay + benefits. WorkStateCode opt-in:
// builder previews without state context get earnings + employer cost lines
// but PT/LWF deductions are skipped (state-dependent).

public sealed record SalaryStructurePreviewQuery(
    decimal AnnualCtc,
    IReadOnlyList<PreviewTemplateComponentInput> TemplateComponents,
    IReadOnlyList<PreviewOverrideInput> Overrides,
    IReadOnlyList<PreviewAddedComponentInput> AddedComponents,
    IReadOnlyList<PreviewBenefitInput> Benefits,
    PreviewEmployeeFlagsInput EmployeeFlags,
    string? WorkStateCode,
    int? Year,
    int? Month
) : IRequest<SalaryStructurePreviewDto>;

public sealed record PreviewTemplateComponentInput(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage,
    int DisplayOrder);

public sealed record PreviewOverrideInput(
    Guid SalaryComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage);

public sealed record PreviewAddedComponentInput(
    Guid ComponentId,
    string FormulaType,
    decimal? FixedAmount,
    decimal? Percentage);

public sealed record PreviewBenefitInput(
    Guid ComponentId,
    decimal AnnualAmount);

public sealed record PreviewEmployeeFlagsInput(
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true,
    bool GratuityEnabled = true,
    bool IsPwd = false);

public sealed record SalaryStructurePreviewDto(
    IReadOnlyList<PreviewRowDto> Rows,
    IReadOnlyList<PreviewEmployerContributionDto> EmployerContributions,
    IReadOnlyList<PreviewEmployeeDeductionDto> EmployeeDeductions,
    decimal NetPayMonthly,
    IReadOnlyList<PreviewBenefitDto> Benefits);

public sealed record PreviewRowDto(
    Guid ComponentId,
    string Code,
    string Name,
    string FormulaType,
    decimal? Percentage,
    decimal? FixedAmount,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    bool IsResidual,
    bool IsAdded,
    bool IsOverride);

public sealed record PreviewEmployerContributionDto(
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed record PreviewEmployeeDeductionDto(
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

public sealed record PreviewBenefitDto(
    Guid ComponentId,
    string Code,
    string Name,
    decimal MonthlyAmount,
    decimal AnnualAmount);

internal sealed class GetSalaryStructurePreviewHandler(
    ISalaryComponentRepository componentRepo,
    IStatutoryConfigRepository statutoryRepo,
    ITenantContext tenantContext)
    : IRequestHandler<SalaryStructurePreviewQuery, SalaryStructurePreviewDto>
{
    public async Task<SalaryStructurePreviewDto> Handle(SalaryStructurePreviewQuery request, CancellationToken ct)
    {
        // Bulk-load every referenced SalaryComponent in a single round-trip.
        IEnumerable<Guid> referencedIds = request.TemplateComponents.Select(c => c.ComponentId)
            .Concat(request.AddedComponents.Select(a => a.ComponentId))
            .Concat(request.Benefits.Select(b => b.ComponentId))
            .Distinct();
        IReadOnlyList<SalaryComponent> components = await componentRepo.GetByIdsAsync(referencedIds.ToList(), ct);
        Dictionary<Guid, SalaryComponent> componentMap = components.ToDictionary(c => c.Id);

        Guid syntheticTemplateId = Guid.NewGuid();
        List<SalaryStructureComponent> templateRows = [];
        foreach (PreviewTemplateComponentInput input in request.TemplateComponents)
        {
            if (!componentMap.TryGetValue(input.ComponentId, out SalaryComponent? component)) continue;
            if (component.TenantId != tenantContext.TenantId) continue;

            ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(input.FormulaType);
            SalaryStructureComponent slot = SalaryStructureComponent.Create(
                syntheticTemplateId, component.Id,
                formulaType, input.FixedAmount, input.Percentage,
                input.DisplayOrder);
            typeof(SalaryStructureComponent).GetProperty("Component")!.SetValue(slot, component);
            templateRows.Add(slot);
        }

        Dictionary<Guid, EmployeeSalaryComponentOverride> overrideMap = [];
        foreach (PreviewOverrideInput ov in request.Overrides)
        {
            ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(ov.FormulaType);
            EmployeeSalaryComponentOverride entity = EmployeeSalaryComponentOverride.Create(
                employeeSalaryStructureId: Guid.NewGuid(),
                salaryComponentId: ov.SalaryComponentId,
                formulaType: formulaType,
                percentage: ov.Percentage,
                fixedAmount: ov.FixedAmount,
                createdBy: Guid.Empty);
            overrideMap[ov.SalaryComponentId] = entity;
        }

        List<SalaryStructurePreviewCalculator.AddedComponent> addedForCalc = [];
        foreach (PreviewAddedComponentInput added in request.AddedComponents)
        {
            if (!componentMap.TryGetValue(added.ComponentId, out SalaryComponent? component)) continue;
            if (component.TenantId != tenantContext.TenantId) continue;

            ComponentFormulaType formulaType = Enum.Parse<ComponentFormulaType>(added.FormulaType);
            EmployeeSalaryComponentOverride ov = EmployeeSalaryComponentOverride.Create(
                employeeSalaryStructureId: Guid.NewGuid(),
                salaryComponentId: added.ComponentId,
                formulaType: formulaType,
                percentage: added.Percentage,
                fixedAmount: added.FixedAmount,
                createdBy: Guid.Empty);
            addedForCalc.Add(new SalaryStructurePreviewCalculator.AddedComponent(
                added.ComponentId, component.Code, component.Name, component.EarningType,
                ConsiderForEpf: component.ConsiderForEpf == true,
                ConsiderForEsi: component.ConsiderForEsi == true,
                Override: ov));
        }

        // Benefits — separate display section.
        List<SalaryStructurePreviewCalculator.BenefitInput> benefitsForCalc = [];
        foreach (PreviewBenefitInput b in request.Benefits)
        {
            if (!componentMap.TryGetValue(b.ComponentId, out SalaryComponent? component)) continue;
            if (component.TenantId != tenantContext.TenantId) continue;
            benefitsForCalc.Add(new SalaryStructurePreviewCalculator.BenefitInput(
                b.ComponentId, component.Code, component.Name, b.AnnualAmount));
        }

        // Build engine StatutoryConfig from real DB rows so the calculator can
        // delegate to engine PFCalculator / ESICalculator / PTCalculator /
        // LWFCalculator. Single source of statutory truth across preview + payroll.
        EngineInputs.StatutoryConfig engineConfig = await BuildEngineConfigAsync(
            request.WorkStateCode, ct);

        SalaryStructurePreviewCalculator.EmployeeStatutoryFlags flags = new(
            EpfEnabled: request.EmployeeFlags.EpfEnabled,
            EsiEnabled: request.EmployeeFlags.EsiEnabled,
            PtEnabled: request.EmployeeFlags.PtEnabled,
            LwfEnabled: request.EmployeeFlags.LwfEnabled,
            GratuityEnabled: request.EmployeeFlags.GratuityEnabled,
            IsPwd: request.EmployeeFlags.IsPwd);

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        SalaryStructurePreviewCalculator.Output result = SalaryStructurePreviewCalculator.Compute(
            new SalaryStructurePreviewCalculator.Inputs(
                AnnualCtc: request.AnnualCtc,
                TemplateComponents: templateRows,
                Overrides: overrideMap,
                AddedComponents: addedForCalc,
                Benefits: benefitsForCalc,
                EmployeeFlags: flags,
                EngineConfig: engineConfig,
                WorkStateCode: request.WorkStateCode,
                Year: request.Year ?? today.Year,
                Month: request.Month ?? today.Month));

        return MapToDto(result);
    }

    private async Task<EngineInputs.StatutoryConfig> BuildEngineConfigAsync(
        string? workStateCode, CancellationToken ct)
    {
        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? StatutoryOrgConfig.CreateDefault(tenantContext.TenantId, Guid.Empty);

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int fiscalYear = today.Month >= 4 ? today.Year : today.Year - 1;
        string fyKey = (fiscalYear + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

        IncomeTaxConfig? taxConfig = await statutoryRepo.GetIncomeTaxConfigAsync(fyKey, "New", ct);
        IReadOnlyList<IncomeTaxSlab> taxSlabs = await statutoryRepo.GetIncomeTaxSlabsAsync(fyKey, "New", ct);
        IReadOnlyList<IncomeTaxSurchargeSlab> surchargeSlabs = await statutoryRepo.GetSurchargeSlabsAsync(fyKey, "New", ct);

        // PT slabs are state-keyed. Only load for the relevant state to keep
        // the payload small; preview only uses one state per call.
        IReadOnlyList<ProfessionalTaxSlab> ptSlabs = !string.IsNullOrWhiteSpace(workStateCode)
            ? await statutoryRepo.GetPtSlabsAsync(workStateCode, today, ct)
            : [];

        // LWF: load all so engine calculator can filter by state internally.
        IReadOnlyList<LwfStateConfig> lwfConfigs = !string.IsNullOrWhiteSpace(workStateCode)
            ? await statutoryRepo.GetLwfConfigsAsync([workStateCode], ct)
            : [];

        return StatutoryConfigBuilder.Build(
            orgConfig, taxConfig, taxSlabs, surchargeSlabs, ptSlabs, lwfConfigs);
    }

    private static SalaryStructurePreviewDto MapToDto(SalaryStructurePreviewCalculator.Output result)
    {
        List<PreviewRowDto> rows = result.Rows.Select(r => new PreviewRowDto(
            r.ComponentId, r.Code, r.Name, r.FormulaType.ToString(),
            r.Percentage, r.FixedAmount, r.MonthlyAmount, r.AnnualAmount,
            r.IsResidual, r.IsAdded, r.IsOverride)).ToList();

        List<PreviewEmployerContributionDto> employerContributions = result.EmployerContributions
            .Select(e => new PreviewEmployerContributionDto(e.Code, e.Name, e.MonthlyAmount, e.AnnualAmount))
            .ToList();

        List<PreviewEmployeeDeductionDto> employeeDeductions = result.EmployeeDeductions
            .Select(d => new PreviewEmployeeDeductionDto(d.Code, d.Name, d.MonthlyAmount, d.AnnualAmount))
            .ToList();

        List<PreviewBenefitDto> benefits = result.Benefits
            .Select(b => new PreviewBenefitDto(Guid.Empty, b.Code, b.Name, b.MonthlyAmount, b.AnnualAmount))
            .ToList();

        return new SalaryStructurePreviewDto(rows, employerContributions, employeeDeductions, result.NetPayMonthly, benefits);
    }
}
