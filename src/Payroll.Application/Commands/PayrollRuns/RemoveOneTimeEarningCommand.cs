using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

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
        breakdownRepo.Remove(target);

        payrunEmp.UpdateComputedAmounts(
            grossPay: payrunEmp.GrossPay - removedAmount,
            netPay: payrunEmp.NetPay - removedAmount,
            taxesAmount: payrunEmp.TaxesAmount,
            benefitsAmount: payrunEmp.BenefitsAmount,
            reimbursementsAmount: payrunEmp.ReimbursementsAmount,
            employeePf: payrunEmp.EmployeePf,
            employerPf: payrunEmp.EmployerPf,
            employeeEsi: payrunEmp.EmployeeEsi,
            employerEsi: payrunEmp.EmployerEsi,
            ptAmount: payrunEmp.PtAmount,
            tdsAmount: payrunEmp.TdsAmount,
            edliAmount: payrunEmp.EdliAmount,
            actorId: req.ActorId);

        payrunEmployeeRepo.Update(payrunEmp);
        await uow.SaveChangesAsync(ct);
    }
}
