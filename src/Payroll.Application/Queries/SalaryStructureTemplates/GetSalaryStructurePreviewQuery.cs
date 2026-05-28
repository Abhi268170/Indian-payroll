using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.SalaryStructureTemplates;

// Backend-authoritative preview of a salary structure. Replaces the per-page
// client-side calculators with a single source of truth — the same
// SalaryStructurePreviewCalculator the persisted-employee query already uses.
//
// Accepts inline template components so the settings builder can preview
// unsaved drafts. For the wizard, the caller passes the same template
// components it already has loaded plus any added/override entries.
//
// Returns the row breakdown + the employer-contribution lines (employer EPF,
// gratuity) that the org's StatutoryOrgConfig says belong in CTC.

public sealed record SalaryStructurePreviewQuery(
    decimal AnnualCtc,
    IReadOnlyList<PreviewTemplateComponentInput> TemplateComponents,
    IReadOnlyList<PreviewOverrideInput> Overrides,
    IReadOnlyList<PreviewAddedComponentInput> AddedComponents,
    PreviewEmployeeFlagsInput EmployeeFlags
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

public sealed record PreviewEmployeeFlagsInput(
    bool EpfEnabled = true,
    bool EsiEnabled = true,
    bool PtEnabled = true,
    bool LwfEnabled = true,
    bool GratuityEnabled = true);

public sealed record SalaryStructurePreviewDto(
    IReadOnlyList<PreviewRowDto> Rows,
    IReadOnlyList<PreviewEmployerContributionDto> EmployerContributions);

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
            .Distinct();
        IReadOnlyList<SalaryComponent> components = await componentRepo.GetByIdsAsync(referencedIds.ToList(), ct);
        Dictionary<Guid, SalaryComponent> componentMap = components.ToDictionary(c => c.Id);

        // Build the in-memory SalaryStructureComponent rows the calculator expects.
        // These are transient — never persisted — but must carry the SalaryComponent
        // navigation so the calculator can read EarningType + ConsiderForEpf.
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

        // Overrides — apply on top of template rows. Wizard passes employee-level
        // edits; builder always passes empty (no employee context yet).
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

        // Added components (not in template) — typically employee-level extra earnings.
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
                Override: ov));
        }

        // Org config — load fresh from DB so the preview reflects the tenant's
        // current employer-EPF-in-CTC + gratuity-in-CTC settings. Falls back to a
        // default config if not yet set (preserves the historical permissive stance).
        StatutoryOrgConfig orgConfig = await statutoryRepo.GetByTenantAsync(ct)
            ?? StatutoryOrgConfig.CreateDefault(tenantContext.TenantId, Guid.Empty);

        SalaryStructurePreviewCalculator.EmployeeStatutoryFlags flags = new(
            EpfEnabled: request.EmployeeFlags.EpfEnabled,
            EsiEnabled: request.EmployeeFlags.EsiEnabled,
            PtEnabled: request.EmployeeFlags.PtEnabled,
            LwfEnabled: request.EmployeeFlags.LwfEnabled,
            GratuityEnabled: request.EmployeeFlags.GratuityEnabled);

        SalaryStructurePreviewCalculator.Output result = SalaryStructurePreviewCalculator.Compute(
            new SalaryStructurePreviewCalculator.Inputs(
                AnnualCtc: request.AnnualCtc,
                TemplateComponents: templateRows,
                Overrides: overrideMap,
                AddedComponents: addedForCalc,
                EmployeeFlags: flags,
                OrgConfig: orgConfig,
                Caps: new SalaryStructurePreviewCalculator.StatutoryCaps()));

        List<PreviewRowDto> rows = result.Rows.Select(r => new PreviewRowDto(
            r.ComponentId, r.Code, r.Name, r.FormulaType.ToString(),
            r.Percentage, r.FixedAmount, r.MonthlyAmount, r.AnnualAmount,
            r.IsResidual, r.IsAdded, r.IsOverride)).ToList();

        List<PreviewEmployerContributionDto> employerContributions = result.EmployerContributions
            .Select(e => new PreviewEmployerContributionDto(e.Code, e.Name, e.MonthlyAmount, e.AnnualAmount))
            .ToList();

        return new SalaryStructurePreviewDto(rows, employerContributions);
    }
}
