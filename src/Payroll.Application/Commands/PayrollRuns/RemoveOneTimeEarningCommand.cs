using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.Entities;

namespace Payroll.Application.Commands.PayrollRuns;

public record RemoveOneTimeEarningCommand(Guid RunId, Guid EmployeeId, Guid BreakdownId, Guid ActorId) : IRequest;

public sealed class RemoveOneTimeEarningCommandValidator : AbstractValidator<RemoveOneTimeEarningCommand>
{
    public RemoveOneTimeEarningCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BreakdownId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class RemoveOneTimeEarningHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryComponentRepository componentRepo,
    IUnitOfWork uow)
    : IRequestHandler<RemoveOneTimeEarningCommand>
{
    public async Task Handle(RemoveOneTimeEarningCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        var allBreakdowns = await breakdownRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct);
        var target = allBreakdowns.FirstOrDefault(b => b.Id == req.BreakdownId)
            ?? throw new NotFoundException($"Breakdown row {req.BreakdownId} not found.");

        if (!target.IsOneTimeEarning)
            throw new InvalidOperationException("Only one-time earnings can be removed.");

        decimal removedAmount = target.FullAmount;
        bool isReimbursement = target.SalaryComponentId is null;

        bool isDeduction = false;
        if (!isReimbursement && target.SalaryComponentId.HasValue)
        {
            SalaryComponent? comp = await componentRepo.GetByIdAsync(target.SalaryComponentId.Value, ct);
            isDeduction = comp?.Category == ComponentCategory.Deduction;
        }

        breakdownRepo.Remove(target);

        payrunEmp.UpdateComputedAmounts(
            grossPay: (isReimbursement || isDeduction) ? payrunEmp.GrossPay : payrunEmp.GrossPay - removedAmount,
            netPay: isDeduction ? payrunEmp.NetPay + removedAmount : payrunEmp.NetPay - removedAmount,
            taxesAmount: payrunEmp.TaxesAmount,
            benefitsAmount: payrunEmp.BenefitsAmount,
            reimbursementsAmount: isReimbursement ? payrunEmp.ReimbursementsAmount - removedAmount : payrunEmp.ReimbursementsAmount,
            employeePf: payrunEmp.EmployeePf,
            employerPf: payrunEmp.EmployerPf,
            employeeEsi: payrunEmp.EmployeeEsi,
            employerEsi: payrunEmp.EmployerEsi,
            ptAmount: payrunEmp.PtAmount,
            tdsAmount: payrunEmp.TdsAmount,
            lwfEmployeeAmount: payrunEmp.LwfEmployeeAmount,
            lwfEmployerAmount: payrunEmp.LwfEmployerAmount,
            gratuityAmount: payrunEmp.GratuityAmount,
            epsAmount: payrunEmp.EpsAmount,
            monthlyCTC: payrunEmp.MonthlyCTC,
            actorId: req.ActorId);

        payrunEmployeeRepo.Update(payrunEmp);
        await uow.SaveChangesAsync(ct);
    }
}
