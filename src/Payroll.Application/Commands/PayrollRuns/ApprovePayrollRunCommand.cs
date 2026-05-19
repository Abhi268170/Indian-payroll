using MediatR;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using System.Text.Json;
using Payroll.Engine;
using Payroll.Engine.Inputs;

namespace Payroll.Application.Commands.PayrollRuns;

public record ApprovePayrollRunCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class ApprovePayrollRunHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IWorkLocationRepository workLocationRepo,
    IPayScheduleRepository payScheduleRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IPayrollRunAuditLogRepository auditLogRepo,
    IUnitOfWork uow)
    : IRequestHandler<ApprovePayrollRunCommand>
{
    public async Task Handle(ApprovePayrollRunCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Only a Draft payroll run can be approved.");

        // Guard: no hard blocks
        var pendingHandler = new GetPendingTasksHandler(runRepo, payrunEmployeeRepo, employeeRepo);
        var pending = await pendingHandler.Handle(new GetPendingTasksQuery(req.RunId), ct);
        if (pending.HasAnyHardBlocks)
            throw new PayrollRunHasBlockingTasksException(pending.HardBlocks.Count);

        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException("Payroll run is missing statutory config snapshot.");

        var staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        int salaryDivisor = PayScheduleHelpers.GetDivisor(engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth, run.PayPeriod.Year, run.PayPeriod.Month);

        var payrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        var activeEmployees = payrunEmployees.Where(pe => pe.Status == PayrunEmployeeStatus.Active).ToList();

        int fiscalYear = run.PayPeriod.FiscalYear;

        foreach (var pe in activeEmployees)
        {
            var employee = await employeeRepo.GetByIdAsync(pe.EmployeeId, ct);
            if (employee is null) continue;

            var workLocation = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
            string workStateCode = workLocation?.State.ToString() ?? "MH";

            var breakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, pe.EmployeeId, ct);
            var result = SetLopCommandHandler.RecomputeEmployee(employee, workStateCode, pe, breakdowns, run, staticConfig, salaryDivisor);

            decimal tdsThisMonth = pe.TdsOverrideAmount ?? result.TDS.MonthlyTDS;
            bool hasPanOverride = string.IsNullOrWhiteSpace(employee.EncryptedPAN);

            var worksheet = TdsWorksheet.Create(
                payrollRunId: req.RunId,
                employeeId: pe.EmployeeId,
                tenantId: run.TenantId,
                fiscalYear: fiscalYear,
                annualProjectedIncome: result.Gross.AnnualProjectedGross,
                standardDeduction: staticConfig.StandardDeduction,
                taxableIncome: result.TDS.TaxableIncome,
                taxBeforeRebate: result.TDS.AnnualProjectedTax,
                rebate87A: result.TDS.Rebate87AApplied ? staticConfig.Rebate87AAmount : 0m,
                surcharge: result.TDS.Surcharge,
                cess: result.TDS.Cess,
                annualTaxLiability: result.TDS.AnnualProjectedTax,
                ytdTdsDeducted: 0m,
                remainingMonthsInFy: run.PayPeriod.MonthsRemainingInFiscalYear(),
                tdsThisMonth: tdsThisMonth,
                hasPanOverride: hasPanOverride,
                createdBy: req.ActorId);

            await tdsWorksheetRepo.AddAsync(worksheet, ct);
        }

        run.Approve(req.ActorId);
        runRepo.Update(run);

        var auditEntry = PayrollRunAuditLog.Create(
            req.RunId, run.TenantId, PayrollRunStatus.Draft, PayrollRunStatus.Approved, req.ActorId, null);
        await auditLogRepo.AddAsync(auditEntry, ct);

        await uow.SaveChangesAsync(ct);
    }
}
