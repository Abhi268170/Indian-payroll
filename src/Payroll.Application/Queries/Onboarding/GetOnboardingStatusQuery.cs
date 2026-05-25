using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Extensions;
using Payroll.Domain.Interfaces;
using Payroll.Domain.Statutory;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;
using EmployeeEntity = Payroll.Domain.Entities.Employee;
using StatutoryOrgConfigEntity = Payroll.Domain.Entities.StatutoryOrgConfig;

namespace Payroll.Application.Queries.Onboarding;

public sealed record GetOnboardingStatusQuery : IRequest<OnboardingStatusDto>;

internal sealed class GetOnboardingStatusHandler(
    IOrgProfileRepository orgProfileRepo,
    IPayScheduleRepository payScheduleRepo,
    IStatutoryConfigRepository statutoryRepo,
    IWorkLocationRepository workLocationRepo,
    IDepartmentRepository departmentRepo,
    IDesignationRepository designationRepo,
    ISalaryStructureTemplateRepository templateRepo,
    IEmployeeRepository employeeRepo,
    ITenantContext tenantContext)
    : IRequestHandler<GetOnboardingStatusQuery, OnboardingStatusDto>
{
    public async Task<OnboardingStatusDto> Handle(GetOnboardingStatusQuery _, CancellationToken ct)
    {
        OrgProfileEntity? org = await orgProfileRepo.GetAsync(ct);
        bool orgComplete = !string.IsNullOrWhiteSpace(org?.CompanyName);
        bool taxDetailsComplete = !string.IsNullOrWhiteSpace(org?.Tan)
            && !string.IsNullOrWhiteSpace(org?.AoAreaCode)
            && !string.IsNullOrWhiteSpace(org?.DeductorType);

        var workLocations = await workLocationRepo.ListAsync(ct);
        int workLocationCount = workLocations.Count;
        bool workLocationsComplete = workLocationCount > 0;

        var depts = await departmentRepo.ListAsync(ct);
        var desigs = await designationRepo.ListAsync(ct);
        bool orgStructureComplete = depts.Count > 0 && desigs.Count > 0;

        var paySchedule = await payScheduleRepo.GetAsync(ct);
        bool payScheduleComplete = paySchedule is not null;
        // Lock fires on RecordPaymentCommand (status flip to Paid). Mirror via the entity flag
        // rather than re-querying payroll runs.
        bool payScheduleLocked = paySchedule?.IsLockedAfterPayrun ?? false;

        StatutoryOrgConfigEntity? statutory = await statutoryRepo.GetByTenantAsync(ct);
        var statutorySubSteps = await BuildStatutorySubStepsAsync(statutory, workLocations, ct);
        // Statutory step is "complete" only when every child sub-step is complete.
        // Mirrors the engine: PT/LWF skipped per-state at run time when no slabs/config
        // apply, so "no PT in this state" = "auto-complete for this state".
        bool statutoryComplete = statutory is not null && statutorySubSteps.All(s => s.Complete);

        var templates = await templateRepo.ListByTenantAsync(tenantContext.TenantId, ct);
        // Completeness requires at least one template that ACTUALLY has component rows —
        // a bare row without components is engine-unusable and would silently break payroll.
        int usableTemplateCount = templates.Count(t => t.Components.Count > 0);
        bool salaryStructureComplete = usableTemplateCount > 0;

        // First-employee check now uses Employee.ProfileComplete (Phase D wired up the
        // RecomputeProfileComplete call sites). The flag is the single source of truth for
        // "this employee will not be silently skipped by the engine". We still expose the
        // raw counts in the step details for the wizard UI.
        var employees = await employeeRepo.ListAsync(ct);
        List<EmployeeEntity> activeEmployees = employees.Where(e => e.Status == EmployeeStatus.Active).ToList();
        int activeCount = activeEmployees.Count;
        int payrollReadyCount = activeEmployees.Count(e => e.ProfileComplete);
        bool firstEmployeeComplete = payrollReadyCount > 0;

        bool deductorComplete = org?.DeductorEmployeeId is not null
            && employees.Any(e => e.Id == org.DeductorEmployeeId && e.Status == EmployeeStatus.Active);

        bool setupComplete =
            orgComplete &&
            workLocationsComplete &&
            orgStructureComplete &&
            payScheduleComplete &&
            statutoryComplete &&
            salaryStructureComplete &&
            firstEmployeeComplete;

        var steps = new List<OnboardingStepDto>
        {
            new("org-profile",       orgComplete,           Required: true,  Skippable: false),
            new("tax-details",       taxDetailsComplete,    Required: false, Skippable: true),
            new("work-locations",    workLocationsComplete, Required: true,  Skippable: false,
                Details: new Dictionary<string, object> { ["count"] = workLocationCount }),
            new("org-structure",     orgStructureComplete,  Required: true,  Skippable: false,
                Details: new Dictionary<string, object> { ["deptCount"] = depts.Count, ["desigCount"] = desigs.Count }),
            new("pay-schedule",      payScheduleComplete,   Required: true,  Skippable: false,
                Details: new Dictionary<string, object> { ["locked"] = payScheduleLocked }),
            new("statutory",         statutoryComplete,     Required: true,  Skippable: false,
                SubSteps: statutorySubSteps),
            new("salary-structure",  salaryStructureComplete, Required: true, Skippable: false,
                Details: new Dictionary<string, object>
                {
                    ["templateCount"] = templates.Count,
                    ["usableTemplateCount"] = usableTemplateCount,
                }),
            new("deductor-employee", deductorComplete,      Required: false, Skippable: true,
                Details: new Dictionary<string, object>
                {
                    ["blockedBy"] = firstEmployeeComplete ? "" : "first-employee",
                }),
            new("first-employee",    firstEmployeeComplete, Required: true,  Skippable: false,
                Details: new Dictionary<string, object>
                {
                    ["activeCount"] = activeCount,
                    ["payrollReadyCount"] = payrollReadyCount,
                }),
        };

        var peopleMissing = new List<string>();
        if (!workLocationsComplete) peopleMissing.Add("work-locations");
        if (depts.Count == 0) peopleMissing.Add("departments");
        if (desigs.Count == 0) peopleMissing.Add("designations");
        if (!salaryStructureComplete) peopleMissing.Add("salary-structure");

        var payRunsMissing = new List<string>();
        if (peopleMissing.Count > 0) payRunsMissing.AddRange(peopleMissing);
        if (!payScheduleComplete) payRunsMissing.Add("pay-schedule");
        if (!statutoryComplete) payRunsMissing.Add("statutory");
        if (!firstEmployeeComplete) payRunsMissing.Add("first-employee");

        var navGates = new Dictionary<string, NavGateDto>
        {
            ["people"] = new(peopleMissing.Count == 0, peopleMissing),
            ["payRuns"] = new(payRunsMissing.Count == 0, payRunsMissing),
        };

        return new OnboardingStatusDto(setupComplete, steps, navGates);
    }

    // Per plan §7: deterministic rules.
    //   EPF — opted out (disabled) OR (enabled + establishment code set).
    //   ESI — same shape.
    //   PT — for each unique work-location state: state has no seeded PT slabs
    //        (PT does not apply there) OR a PtStateRegistration row exists.
    //   LWF — for each unique work-location state: state has no seeded LWF
    //        config (LWF does not apply) OR a tenant LwfStateConfig row exists.
    //   Statutory Bonus — boolean; default seed sets it true so complete out
    //        of the box, but we still surface it for visibility.
    // No hardcoded state lists — PT/LWF applicability is read from the seeded
    // statutory tables so the rules track whatever the seed says.
    private async Task<List<OnboardingSubStepDto>> BuildStatutorySubStepsAsync(
        StatutoryOrgConfigEntity? statutory,
        IReadOnlyList<Domain.Entities.WorkLocation> workLocations,
        CancellationToken ct)
    {
        var rows = new List<OnboardingSubStepDto>();

        // EPF
        if (statutory is null)
        {
            rows.Add(new OnboardingSubStepDto("epf", "Provident Fund (EPF)", Complete: false,
                Hint: "Statutory configuration not initialised — contact support."));
        }
        else if (!statutory.EpfEnabled)
        {
            rows.Add(new OnboardingSubStepDto("epf", "Provident Fund (EPF)", Complete: true));
        }
        else if (string.IsNullOrWhiteSpace(statutory.EpfEstablishmentCode))
        {
            rows.Add(new OnboardingSubStepDto("epf", "Provident Fund (EPF)", Complete: false,
                Hint: "EPF is enabled but the establishment code is missing."));
        }
        else
        {
            rows.Add(new OnboardingSubStepDto("epf", "Provident Fund (EPF)", Complete: true));
        }

        // ESI
        if (statutory is null)
        {
            rows.Add(new OnboardingSubStepDto("esi", "State Insurance (ESI)", Complete: false));
        }
        else if (!statutory.EsiEnabled)
        {
            rows.Add(new OnboardingSubStepDto("esi", "State Insurance (ESI)", Complete: true));
        }
        else if (string.IsNullOrWhiteSpace(statutory.EsiEstablishmentCode))
        {
            rows.Add(new OnboardingSubStepDto("esi", "State Insurance (ESI)", Complete: false,
                Hint: "ESI is enabled but the establishment code is missing."));
        }
        else
        {
            rows.Add(new OnboardingSubStepDto("esi", "State Insurance (ESI)", Complete: true));
        }

        // PT — per unique work-location state.
        var states = workLocations.Where(w => w.IsActive).Select(w => w.State).Distinct().ToList();
        if (states.Count == 0)
        {
            rows.Add(new OnboardingSubStepDto("pt", "Professional Tax", Complete: false,
                Hint: "Add a work location to derive Professional Tax applicability."));
        }
        else
        {
            var ptMissing = new List<string>();
            var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var state in states)
            {
                string code = state.ToIsoCode();
                var slabs = await statutoryRepo.GetPtSlabsAsync(code, asOf, ct);
                if (slabs.Count == 0) continue;  // state has no PT — auto-complete
                var registration = await statutoryRepo.GetPtRegistrationAsync(code, ct);
                if (registration is null) ptMissing.Add(state.ToString());
            }
            if (ptMissing.Count == 0)
            {
                rows.Add(new OnboardingSubStepDto("pt", "Professional Tax", Complete: true));
            }
            else
            {
                rows.Add(new OnboardingSubStepDto("pt", "Professional Tax", Complete: false,
                    Hint: $"Add PT registration for: {string.Join(", ", ptMissing)}."));
            }
        }

        // LWF — per unique work-location state.
        //
        // Two-part check:
        //   1. Is the state on the regulated-LWF list (StatutoryReference)? If no,
        //      LWF does not apply → auto-complete for that state.
        //   2. If yes, is the seed-expected LwfStateConfig row present in the
        //      tenant schema? Missing row = drift (provisioner failed, manual
        //      delete, etc.) → incomplete with a per-state hint so the operator
        //      knows exactly which state to fix.
        if (states.Count == 0)
        {
            rows.Add(new OnboardingSubStepDto("lwf", "Labour Welfare Fund", Complete: false,
                Hint: "Add a work location to derive Labour Welfare Fund applicability."));
        }
        else
        {
            var stateCodes = states.Select(s => s.ToIsoCode()).Distinct().ToList();
            var configs = await statutoryRepo.GetLwfConfigsAsync(stateCodes, ct);
            var configuredStates = configs.Select(c => c.StateCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var driftStates = states
                .Where(s => StatutoryReference.LwfApplicableStates.Contains(s.ToIsoCode()))
                .Where(s => !configuredStates.Contains(s.ToIsoCode()))
                .Select(s => s.ToString())
                .ToList();
            if (driftStates.Count == 0)
            {
                rows.Add(new OnboardingSubStepDto("lwf", "Labour Welfare Fund", Complete: true));
            }
            else
            {
                rows.Add(new OnboardingSubStepDto("lwf", "Labour Welfare Fund", Complete: false,
                    Hint: $"Configure LWF for: {string.Join(", ", driftStates)}."));
            }
        }

        // Statutory Bonus — boolean toggle on org config.
        if (statutory is null)
        {
            rows.Add(new OnboardingSubStepDto("bonus", "Statutory Bonus", Complete: false));
        }
        else
        {
            rows.Add(new OnboardingSubStepDto("bonus", "Statutory Bonus", Complete: true));
        }

        return rows;
    }
}
