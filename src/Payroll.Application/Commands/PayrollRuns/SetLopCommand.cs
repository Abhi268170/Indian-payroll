using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;
using System.Text.Json;

namespace Payroll.Application.Commands.PayrollRuns;

public record SetLopCommand(Guid RunId, Guid EmployeeId, int LopDays, Guid ActorId) : IRequest;

public sealed class SetLopCommandValidator : AbstractValidator<SetLopCommand>
{
    public SetLopCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.LopDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class SetLopCommandHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    IWorkLocationRepository workLocationRepo,
    IPayScheduleRepository payScheduleRepo,
    IEmployeeFyOpeningRepository fyOpeningRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IUnitOfWork uow)
    : IRequestHandler<SetLopCommand>
{
    public async Task Handle(SetLopCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot set LOP for a skipped employee.");

        var paySchedule = await payScheduleRepo.GetAsync(ct)
            ?? throw new DomainException("Pay Schedule not configured.");
        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        EngineSalaryCalculationMethod engineCalcMethod = paySchedule.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays
            ? EngineSalaryCalculationMethod.ActualDays
            : EngineSalaryCalculationMethod.FixedDays;
        int salaryDivisor = PayScheduleHelpers.GetDivisor(engineCalcMethod, paySchedule.FixedWorkingDaysPerMonth, run.PayPeriod.Year, run.PayPeriod.Month);

        // Guard — LOP must not exceed the salary divisor to prevent negative net pay
        if (req.LopDays >= salaryDivisor)
            throw new InvalidOperationException($"LOP days ({req.LopDays}) must be less than the salary divisor ({salaryDivisor}).");

        payrunEmp.SetLop(req.LopDays, req.ActorId);

        // Re-run engine with new LOP using stored component full amounts
        var breakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        var workLocation = await workLocationRepo.GetByIdAsync(employee.WorkLocationId, ct);
        string workStateCode = workLocation?.State.ToString() ?? "MH";

        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException("Payroll run is missing statutory config snapshot.");
        var staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;

        Dictionary<Guid, (decimal YtdGross, decimal YtdTds)> currentYtdMap =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync([req.EmployeeId], run.PayPeriod.FiscalYear, ct);

        EmployeeFyOpening? fyOpening = await fyOpeningRepo.GetAsync(req.EmployeeId, run.PayPeriod.FiscalYear, ct);
        if (fyOpening is not null)
        {
            currentYtdMap.TryGetValue(req.EmployeeId, out var existing);
            currentYtdMap[req.EmployeeId] = (
                existing.YtdGross + fyOpening.GrossSalary,
                existing.YtdTds + fyOpening.TdsDeducted);
        }

        currentYtdMap.TryGetValue(req.EmployeeId, out (decimal YtdGross, decimal YtdTds) currentYtd);

        PayrollResult result = RecomputeEmployee(employee, workStateCode, payrunEmp, breakdowns, run, staticConfig, salaryDivisor, currentYtd.YtdGross, currentYtd.YtdTds);

        payrunEmp.UpdateComputedAmounts(
            grossPay: result.Gross.GrossWage,
            netPay: result.NetPay,
            taxesAmount: result.TDS.MonthlyTDS + result.PT.Amount,
            benefitsAmount: result.PF.EPFEmployerContribution + result.ESI.EmployerContribution,
            reimbursementsAmount: 0m,
            employeePf: result.PF.EmployeeContribution,
            employerPf: result.PF.EPFEmployerContribution,
            employeeEsi: result.ESI.EmployeeContribution,
            employerEsi: result.ESI.EmployerContribution,
            ptAmount: result.PT.Amount,
            tdsAmount: result.TDS.MonthlyTDS,
            lwfEmployeeAmount: result.LWF.EmployeeAmount,
            lwfEmployerAmount: result.LWF.EmployerAmount,
            gratuityAmount: result.Gratuity.MonthlyAccrual,
            epsAmount: result.PF.EPSEmployerContribution,
            monthlyCTC: payrunEmp.MonthlyCTC,
            actorId: req.ActorId);

        // Update prorated amounts on salary component breakdowns
        foreach (var bd in breakdowns.Where(b => !b.IsOneTimeEarning))
        {
            var computed = result.Gross.ComponentBreakdown.FirstOrDefault(c => c.ComponentId == bd.SalaryComponentId);
            if (computed is not null)
                bd.UpdateAmounts(computed.FullAmount, computed.ProratedAmount);
        }

        payrunEmployeeRepo.Update(payrunEmp);

        // Upsert TDS worksheet
        await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        await tdsWorksheetRepo.AddAsync(Domain.Entities.TdsWorksheet.Create(
            payrollRunId: req.RunId,
            employeeId: req.EmployeeId,
            tenantId: payrunEmp.TenantId,
            fiscalYear: run.PayPeriod.FiscalYear,
            annualProjectedIncome: result.TDS.TaxableIncome + staticConfig.StandardDeduction,
            standardDeduction: staticConfig.StandardDeduction,
            taxableIncome: result.TDS.TaxableIncome,
            taxBeforeRebate: result.TDS.TaxBeforeRebate,
            rebate87A: result.TDS.Rebate87AApplied ? Math.Min(result.TDS.TaxBeforeRebate, staticConfig.Rebate87AAmount) : 0m,
            surcharge: result.TDS.Surcharge,
            cess: result.TDS.Cess,
            annualTaxLiability: result.TDS.AnnualProjectedTax,
            ytdTdsDeducted: 0m,
            remainingMonthsInFy: run.PayPeriod.MonthsRemainingInFiscalYear(),
            tdsThisMonth: result.TDS.MonthlyTDS,
            hasPanOverride: result.TDS.HasPanOverride,
            createdBy: req.ActorId), ct);

        var allEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        var activeEmployees = allEmployees.Where(e => e.Status == PayrunEmployeeStatus.Active).ToList();
        run.UpdateFinancialSummary(
            payrollCost: activeEmployees.Sum(e => e.GrossPay + e.EmployerPf + e.EmployerEsi),
            totalNetPay: activeEmployees.Sum(e => e.NetPay),
            totalEmployerPf: activeEmployees.Sum(e => e.EmployerPf),
            totalEmployerEsi: activeEmployees.Sum(e => e.EmployerEsi),
            totalTds: activeEmployees.Sum(e => e.TdsAmount),
            totalPt: activeEmployees.Sum(e => e.PtAmount),
            employeeCount: activeEmployees.Count,
            actorId: req.ActorId);
        runRepo.Update(run);

        await uow.SaveChangesAsync(ct);
    }

    internal static PayrollResult RecomputeEmployee(
        Domain.Entities.Employee employee,
        string workStateCode,
        Domain.Entities.PayrunEmployee payrunEmp,
        IReadOnlyList<Domain.Entities.PayrunComponentBreakdown> breakdowns,
        Domain.Entities.PayrollRun run,
        StatutoryConfig staticConfig,
        int salaryDivisor,
        decimal currentEmployerYtdGross = 0m,
        decimal currentEmployerYtdTds = 0m)
    {
        // Build components from stored full amounts (excludes one-time earnings — handled separately)
        var components = breakdowns
            .Where(b => !b.IsOneTimeEarning)
            .Select(b => new SalaryComponentInput(b.SalaryComponentId ?? Guid.Empty, b.ComponentCode, b.FullAmount, IsTaxable: true, ConsiderForEpf: b.ConsiderForEpf))
            .ToList();

        decimal basicWage = breakdowns
            .FirstOrDefault(b => b.ComponentCode == "BASICSALARY")?.FullAmount ?? 0m;

        bool hasPan = !string.IsNullOrWhiteSpace(employee.EncryptedPAN);
        var (hyIndex, hyTotal) = run.PayPeriod.HalfYearPosition(employee.DateOfJoining);
        var empInput = new EmployeeInput(
            EmployeeId: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            WorkStateCode: workStateCode,
            EpfEnabled: employee.EpfEnabled,
            IsESIExempt: !employee.EsiEnabled,
            IsPWD: employee.IsPWD,
            MonthlyCTC: 0m,
            Components: components,
            LOPDays: payrunEmp.LopDays,
            WorkingDaysInMonth: payrunEmp.BaseDays,
            VPFAmount: 0m,
            PriorEmployerYTDTaxableIncome: 0m,
            PriorEmployerYTDTDSDeducted: 0m,
            PriorEmployerYTDPF: 0m,
            HalfYearMonthIndex: hyIndex,
            HalfYearTotalMonths: hyTotal,
            BasicWage: basicWage,
            HasPan: hasPan,
            CurrentEmployerYTDGross: currentEmployerYtdGross,
            CurrentEmployerYTDTDSDeducted: currentEmployerYtdTds);

        var runInput = new PayrollRunInput(
            Year: run.PayPeriod.Year,
            Month: run.PayPeriod.Month,
            CalendarDaysInMonth: payrunEmp.BaseDays,
            SalaryDivisor: salaryDivisor,
            MonthsRemainingInFY: run.PayPeriod.MonthsRemainingInFiscalYear(),
            FiscalYearLabel: run.PayPeriod.FiscalYearLabel);

        return PayrollEngine.Compute([empInput], runInput, staticConfig)[0];
    }
}
