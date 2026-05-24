using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
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
    IEmployeeSalaryStructureRepository salaryStructureRepo,
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
        bool statutoryComplete = statutory is not null;

        var templates = await templateRepo.ListByTenantAsync(tenantContext.TenantId, ct);
        bool salaryStructureComplete = templates.Count > 0;

        // First-employee check uses concrete field presence (DateOfBirth, FathersName,
        // EncryptedBankAccount, active salary structure) per the plan §5.4 — does NOT depend
        // on Employee.ProfileComplete because that flag is not wired in production today.
        // One ListAsync + one batched lookup for active structures avoids per-employee N+1.
        var employees = await employeeRepo.ListAsync(ct);
        List<EmployeeEntity> activeEmployees = employees.Where(e => e.Status == EmployeeStatus.Active).ToList();
        int activeCount = activeEmployees.Count;
        List<EmployeeEntity> fieldReady = activeEmployees
            .Where(e => e.DateOfBirth != default
                     && !string.IsNullOrWhiteSpace(e.FathersName)
                     && !string.IsNullOrWhiteSpace(e.EncryptedBankAccount))
            .ToList();
        HashSet<Guid> withStructure = await salaryStructureRepo
            .GetEmployeesWithActiveStructureAsync(fieldReady.Select(e => e.Id), ct);
        int payrollReadyCount = fieldReady.Count(e => withStructure.Contains(e.Id));
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
            new("statutory",         statutoryComplete,     Required: true,  Skippable: false),
            new("salary-structure",  salaryStructureComplete, Required: true, Skippable: false,
                Details: new Dictionary<string, object> { ["templateCount"] = templates.Count }),
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
}
