using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using OrgProfileEntity = Payroll.Domain.Entities.OrgProfile;
using EmployeeEntity = Payroll.Domain.Entities.Employee;

namespace Payroll.Application.Queries.PayrollRuns;

public sealed record GetPayrollRunPreflightQuery : IRequest<PayrollRunPreflightDto>;

internal sealed class GetPayrollRunPreflightHandler(
    IOrgProfileRepository orgProfileRepo,
    IPayScheduleRepository payScheduleRepo,
    IStatutoryConfigRepository statutoryRepo,
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryStructureRepo)
    : IRequestHandler<GetPayrollRunPreflightQuery, PayrollRunPreflightDto>
{
    public async Task<PayrollRunPreflightDto> Handle(GetPayrollRunPreflightQuery _, CancellationToken ct)
    {
        var blockers = new List<PreflightBlockerDto>();
        var warnings = new List<PreflightWarningDto>();

        var paySchedule = await payScheduleRepo.GetAsync(ct);
        if (paySchedule is null)
        {
            blockers.Add(new PreflightBlockerDto(
                Code: "PAY_SCHEDULE_MISSING",
                Message: "Pay Schedule not configured.",
                FixUrl: "/settings/pay-schedule"));
        }

        var statutory = await statutoryRepo.GetByTenantAsync(ct);
        if (statutory is null)
        {
            blockers.Add(new PreflightBlockerDto(
                Code: "STATUTORY_MISSING",
                Message: "Statutory configuration not found.",
                FixUrl: "/settings/statutory"));
        }

        // One ListAsync + one batched structure lookup avoids per-employee N+1.
        var employees = await employeeRepo.ListAsync(ct);
        List<EmployeeEntity> activeEmployees = employees.Where(e => e.Status == EmployeeStatus.Active).ToList();
        List<EmployeeEntity> fieldReady = activeEmployees
            .Where(e => e.DateOfBirth != default
                     && !string.IsNullOrWhiteSpace(e.FathersName)
                     && !string.IsNullOrWhiteSpace(e.EncryptedBankAccount))
            .ToList();
        HashSet<Guid> withStructure = await salaryStructureRepo
            .GetEmployeesWithActiveStructureAsync(fieldReady.Select(e => e.Id), ct);
        int payrollReadyCount = fieldReady.Count(e => withStructure.Contains(e.Id));
        if (payrollReadyCount == 0)
        {
            blockers.Add(new PreflightBlockerDto(
                Code: "NO_PAYABLE_EMPLOYEES",
                Message: $"No employees with payroll-ready profile (need Date of Birth, Father's Name, bank account, and active salary structure). Active employees: {activeEmployees.Count}, field-complete: {fieldReady.Count}.",
                FixUrl: "/employees",
                Count: activeEmployees.Count));
        }

        OrgProfileEntity? org = await orgProfileRepo.GetAsync(ct);
        // Company PAN lives on OrgProfile (single source of truth), but it's required for
        // Form 24Q / Form 16 just as much as TAN and AO code — surface the gap here so the
        // operator sees one consolidated tax-readiness warning instead of having to discover
        // the PAN field is missing only when filing.
        if (string.IsNullOrWhiteSpace(org?.Pan))
        {
            warnings.Add(new PreflightWarningDto(
                Code: "ORG_PAN_MISSING",
                Message: "Company PAN is not set. Form 24Q and Form 16 cannot be filed without it.",
                FixUrl: "/settings/org-profile"));
        }
        bool taxDetailsComplete = !string.IsNullOrWhiteSpace(org?.Tan)
            && !string.IsNullOrWhiteSpace(org?.AoAreaCode)
            && !string.IsNullOrWhiteSpace(org?.DeductorType);
        if (!taxDetailsComplete)
        {
            warnings.Add(new PreflightWarningDto(
                Code: "TAX_DETAILS_INCOMPLETE",
                Message: "Form 24Q and Form 16 will be unavailable until Tax Details are set.",
                FixUrl: "/settings/tax-details"));
        }
        if (org?.DeductorEmployeeId is null)
        {
            warnings.Add(new PreflightWarningDto(
                Code: "DEDUCTOR_EMPLOYEE_MISSING",
                Message: "Tax Deductor employee not assigned. Required signatory for Form 16.",
                FixUrl: "/settings/tax-details"));
        }

        return new PayrollRunPreflightDto(
            Ready: blockers.Count == 0,
            Blockers: blockers,
            Warnings: warnings);
    }
}
