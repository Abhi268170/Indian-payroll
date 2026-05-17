using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record AddOneTimeEarningCommand(
    Guid RunId,
    Guid EmployeeId,
    Guid ComponentId,
    decimal Amount,
    Guid ActorId) : IRequest<Guid>;

public sealed class AddOneTimeEarningCommandValidator : AbstractValidator<AddOneTimeEarningCommand>
{
    public AddOneTimeEarningCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class AddOneTimeEarningHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ISalaryComponentRepository componentRepo,
    IUnitOfWork uow)
    : IRequestHandler<AddOneTimeEarningCommand, Guid>
{
    public async Task<Guid> Handle(AddOneTimeEarningCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Variable inputs can only be changed on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        if (payrunEmp.Status == PayrunEmployeeStatus.Skipped)
            throw new InvalidOperationException("Cannot add earnings for a skipped employee.");

        var component = await componentRepo.GetByIdAsync(req.ComponentId, ct)
            ?? throw new NotFoundException($"Salary component {req.ComponentId} not found.");

        var breakdown = PayrunComponentBreakdown.Create(
            run.Id, req.EmployeeId, run.TenantId,
            req.ComponentId, component.Code, component.Name ?? component.Code,
            req.Amount, req.Amount, isOneTimeEarning: true);

        await breakdownRepo.AddAsync(breakdown, ct);

        // One-time earnings add to gross and net; they don't affect PF/ESI/PT/TDS in this model
        payrunEmp.UpdateComputedAmounts(
            grossPay: payrunEmp.GrossPay + req.Amount,
            netPay: payrunEmp.NetPay + req.Amount,
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

        return breakdown.Id;
    }
}
