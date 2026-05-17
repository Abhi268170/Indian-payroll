using FluentValidation;
using MediatR;
using Payroll.Application.Services;
using Payroll.Domain.Common;
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

        // Domain guard — LOP must be < baseDays
        if (req.LopDays >= payrunEmp.BaseDays)
            throw new InvalidOperationException($"LOP days ({req.LopDays}) must be less than base days ({payrunEmp.BaseDays}).");

        payrunEmp.SetLop(req.LopDays, req.ActorId);

        // Re-run engine with new LOP using stored component full amounts
        var breakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException("Payroll run is missing statutory config snapshot.");
        var staticConfig = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)!;
        PayrollResult result = RecomputeEmployee(employee, payrunEmp, breakdowns, run, staticConfig);

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
            edliAmount: result.PF.EDLIEmployerContribution,
            actorId: req.ActorId);

        // Update prorated amounts on salary component breakdowns
        foreach (var bd in breakdowns.Where(b => !b.IsOneTimeEarning))
        {
            var computed = result.Gross.ComponentBreakdown.FirstOrDefault(c => c.ComponentId == bd.SalaryComponentId);
            if (computed is not null)
                bd.UpdateAmounts(computed.FullAmount, computed.ProratedAmount);
        }

        payrunEmployeeRepo.Update(payrunEmp);
        await uow.SaveChangesAsync(ct);
    }

    internal static PayrollResult RecomputeEmployee(
        Domain.Entities.Employee employee,
        Domain.Entities.PayrunEmployee payrunEmp,
        IReadOnlyList<Domain.Entities.PayrunComponentBreakdown> breakdowns,
        Domain.Entities.PayrollRun run,
        StatutoryConfig staticConfig)
    {
        // Build components from stored full amounts (excludes one-time earnings — handled separately)
        var components = breakdowns
            .Where(b => !b.IsOneTimeEarning)
            .Select(b => new SalaryComponentInput(b.SalaryComponentId ?? Guid.Empty, b.ComponentCode, b.FullAmount, true))
            .ToList();

        string workState = employee.ResidentialState?.ToString() ?? "MH";
        var empInput = new EmployeeInput(
            EmployeeId: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            WorkStateCode: workState,
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
            PriorEmployerYTDPF: 0m);

        var runInput = new PayrollRunInput(
            Year: run.PayPeriod.Year,
            Month: run.PayPeriod.Month,
            CalendarDaysInMonth: payrunEmp.BaseDays,
            MonthsRemainingInFY: run.PayPeriod.MonthsRemainingInFiscalYear(),
            FiscalYearLabel: run.PayPeriod.FiscalYearLabel);

        return PayrollEngine.Compute([empInput], runInput, staticConfig)[0];
    }
}
