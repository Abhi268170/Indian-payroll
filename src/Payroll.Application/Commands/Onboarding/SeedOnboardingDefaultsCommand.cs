using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Onboarding;

/// Seeds suggested defaults for a single onboarding step. Idempotent: skips work that
/// has already been done (e.g. won't add "Engineering" twice). Routes per step:
///   - "work-locations" → adds a "Head Office" work location using OrgProfile state if
///     none exist.
///   - "org-structure" → adds default Engineering / Operations departments and
///     Software Engineer / Manager designations if none exist.
///   - "salary-structure" → creates a "Standard" template from seeded Basic + HRA +
///     Special Allowance components if none exist.
public sealed record SeedOnboardingDefaultsCommand(string Step, Guid ActorId) : IRequest;

internal sealed class SeedOnboardingDefaultsHandler(
    IOrgProfileRepository orgProfileRepo,
    IWorkLocationRepository workLocationRepo,
    IDepartmentRepository departmentRepo,
    IDesignationRepository designationRepo,
    ISalaryComponentRepository salaryComponentRepo,
    ISalaryStructureTemplateRepository templateRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<SeedOnboardingDefaultsCommand>
{
    public async Task Handle(SeedOnboardingDefaultsCommand req, CancellationToken ct)
    {
        switch (req.Step)
        {
            case "work-locations":
                await SeedWorkLocationAsync(req.ActorId, ct);
                break;
            case "org-structure":
                await SeedOrgStructureAsync(req.ActorId, ct);
                break;
            case "salary-structure":
                await SeedSalaryStructureAsync(req.ActorId, ct);
                break;
            default:
                throw new DomainException($"Unknown onboarding step '{req.Step}' for seed-defaults.");
        }
        await uow.SaveChangesAsync(ct);
    }

    private async Task SeedWorkLocationAsync(Guid actorId, CancellationToken ct)
    {
        var existing = await workLocationRepo.ListAsync(ct);
        if (existing.Count > 0) return;

        var org = await orgProfileRepo.GetAsync(ct);
        // Fallback to a neutral state if OrgProfile hasn't been filled yet. Operators can
        // edit afterwards via the Work Locations settings page.
        IndianState state = org?.State ?? IndianState.Karnataka;
        var wl = WorkLocation.Create(
            name: "Head Office",
            state: state,
            addressLine1: org?.AddressLine1,
            addressLine2: org?.AddressLine2,
            city: org?.City,
            pinCode: org?.PinCode,
            createdBy: actorId);
        await workLocationRepo.AddAsync(wl, ct);
    }

    private async Task SeedOrgStructureAsync(Guid actorId, CancellationToken ct)
    {
        var depts = await departmentRepo.ListAsync(ct);
        if (depts.Count == 0)
        {
            await departmentRepo.AddAsync(Department.Create("Engineering", null, null, actorId), ct);
            await departmentRepo.AddAsync(Department.Create("Operations", null, null, actorId), ct);
        }
        var desigs = await designationRepo.ListAsync(ct);
        if (desigs.Count == 0)
        {
            await designationRepo.AddAsync(Designation.Create("Software Engineer", actorId), ct);
            await designationRepo.AddAsync(Designation.Create("Manager", actorId), ct);
        }
    }

    private async Task SeedSalaryStructureAsync(Guid actorId, CancellationToken ct)
    {
        // Strong idempotency: only skip if there is a template that ALREADY has at least
        // one component. A previous run that crashed between the template save and the
        // component save would have left an empty template — repair it rather than skip.
        var existing = await templateRepo.ListByTenantAsync(tenantContext.TenantId, ct);
        if (existing.Any(t => t.Components.Count > 0)) return;

        var components = await salaryComponentRepo.ListByTenantAsync(tenantContext.TenantId, null, ct);
        SalaryComponent? basic = components.FirstOrDefault(c => c.Code.Equals("BASICSALARY", StringComparison.OrdinalIgnoreCase));
        SalaryComponent? hra = components.FirstOrDefault(c => c.Code.Equals("HOUSERENTALLOWANCE", StringComparison.OrdinalIgnoreCase));
        SalaryComponent? residual = components.FirstOrDefault(c => c.FormulaType == ComponentFormulaType.ResidualCTC);
        if (basic is null || residual is null)
        {
            // Seeded components missing — provisioning didn't run as expected. Surface a
            // clear error rather than creating a half-broken template.
            throw new DomainException("Default salary components are missing. Re-run tenant provisioning.");
        }

        // Repair path: existing empty template — top up its components in place.
        SalaryStructureTemplate? emptyTemplate = existing.FirstOrDefault(t => t.Components.Count == 0);
        if (emptyTemplate is not null)
        {
            var repairRows = BuildDefaultComponents(emptyTemplate.Id, basic, hra, residual);
            emptyTemplate.SetComponents(repairRows);
            // Single SaveChanges in the outer handler commits parent + children together.
            return;
        }

        // Fresh path: build template + components, attach via the HasMany navigation, then
        // a single SaveChanges (in the outer handler) commits both in one transaction.
        // Id is assigned at construction (AuditableEntity), so children can reference it
        // before the row is persisted.
        var template = SalaryStructureTemplate.Create(
            "Standard",
            "Default structure created by onboarding wizard.",
            tenantContext.TenantId,
            actorId);
        template.SetComponents(BuildDefaultComponents(template.Id, basic, hra, residual));
        await templateRepo.AddAsync(template, ct);
    }

    private static List<SalaryStructureComponent> BuildDefaultComponents(
        Guid templateId,
        SalaryComponent basic,
        SalaryComponent? hra,
        SalaryComponent residual)
    {
        var rows = new List<SalaryStructureComponent>
        {
            SalaryStructureComponent.Create(templateId, basic.Id, ComponentFormulaType.PercentOfCTC, fixedAmount: null, percentage: 40m, displayOrder: 1),
        };
        int order = 2;
        if (hra is not null)
        {
            rows.Add(SalaryStructureComponent.Create(templateId, hra.Id, ComponentFormulaType.PercentOfBasic, fixedAmount: null, percentage: 40m, displayOrder: order++));
        }
        rows.Add(SalaryStructureComponent.Create(templateId, residual.Id, ComponentFormulaType.ResidualCTC, fixedAmount: null, percentage: null, displayOrder: order));
        return rows;
    }
}
