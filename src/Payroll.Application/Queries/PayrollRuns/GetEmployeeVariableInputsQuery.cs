using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetEmployeeVariableInputsQuery(Guid RunId, Guid EmployeeId) : IRequest<EmployeeVariableInputsDto>;

internal sealed class GetEmployeeVariableInputsHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryComponentRepository componentRepo)
    : IRequestHandler<GetEmployeeVariableInputsQuery, EmployeeVariableInputsDto>
{
    public async Task<EmployeeVariableInputsDto> Handle(GetEmployeeVariableInputsQuery req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        var breakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);

        // Build component-id → category map for deduction detection
        var allComponents = await componentRepo.ListByTenantAsync(run.TenantId, ct: ct);
        var deductionIds = allComponents
            .Where(c => c.Category == ComponentCategory.Deduction)
            .Select(c => c.Id)
            .ToHashSet();

        var components = breakdowns
            .Select(b => new ComponentBreakdownDto(
                Id: b.Id,
                SalaryComponentId: b.SalaryComponentId ?? Guid.Empty,
                ComponentCode: b.ComponentCode,
                ComponentName: b.ComponentName,
                FullAmount: b.FullAmount,
                ProratedAmount: b.ProratedAmount,
                IsOneTimeEarning: b.IsOneTimeEarning,
                IsDeduction: b.SalaryComponentId.HasValue && deductionIds.Contains(b.SalaryComponentId.Value)))
            .ToList();

        return new EmployeeVariableInputsDto(
            PayrollRunId: req.RunId,
            EmployeeId: req.EmployeeId,
            LopDays: payrunEmp.LopDays,
            BaseDays: payrunEmp.BaseDays,
            ActualPayableDays: payrunEmp.ActualPayableDays,
            GrossPay: payrunEmp.GrossPay,
            NetPay: payrunEmp.NetPay,
            TdsAmount: payrunEmp.TdsAmount,
            TdsOverrideAmount: payrunEmp.TdsOverrideAmount,
            TdsOverrideReason: payrunEmp.TdsOverrideReason,
            EmployeePf: payrunEmp.EmployeePf,
            EmployerPf: payrunEmp.EmployerPf,
            EmployeeEsi: payrunEmp.EmployeeEsi,
            EmployerEsi: payrunEmp.EmployerEsi,
            PtAmount: payrunEmp.PtAmount,
            LwfEmployeeAmount: payrunEmp.LwfEmployeeAmount,
            LwfEmployerAmount: payrunEmp.LwfEmployerAmount,
            GratuityAmount: payrunEmp.GratuityAmount,
            EpsAmount: payrunEmp.EpsAmount,
            MonthlyCTC: payrunEmp.MonthlyCTC,
            Components: components);
    }
}
