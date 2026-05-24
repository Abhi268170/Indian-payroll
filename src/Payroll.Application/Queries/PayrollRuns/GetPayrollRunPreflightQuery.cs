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

        var employees = await employeeRepo.ListAsync(ct);
        int payrollReadyCount = 0;
        foreach (EmployeeEntity emp in employees.Where(e => e.Status == EmployeeStatus.Active))
        {
            if (emp.DateOfBirth == default) continue;
            if (string.IsNullOrWhiteSpace(emp.FathersName)) continue;
            if (string.IsNullOrWhiteSpace(emp.EncryptedBankAccount)) continue;
            var structure = await salaryStructureRepo.GetActiveAsync(emp.Id, ct);
            if (structure is null) continue;
            payrollReadyCount++;
        }
        if (payrollReadyCount == 0)
        {
            blockers.Add(new PreflightBlockerDto(
                Code: "NO_PAYABLE_EMPLOYEES",
                Message: "No employees with payroll-ready profile (need Date of Birth, Father's Name, bank account, and active salary structure).",
                FixUrl: "/employees",
                Count: 0));
        }

        OrgProfileEntity? org = await orgProfileRepo.GetAsync(ct);
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
