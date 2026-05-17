using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record UndoSkipEmployeeCommand(Guid RunId, Guid EmployeeId, Guid ActorId) : IRequest;

public sealed class UndoSkipEmployeeCommandValidator : AbstractValidator<UndoSkipEmployeeCommand>
{
    public UndoSkipEmployeeCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class UndoSkipEmployeeHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IUnitOfWork uow)
    : IRequestHandler<UndoSkipEmployeeCommand>
{
    public async Task Handle(UndoSkipEmployeeCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Skip can only be undone on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        payrunEmp.UndoSkip(req.ActorId);
        payrunEmployeeRepo.Update(payrunEmp);

        await SkipEmployeeHandler.RecalculateRunTotals(run, req.RunId, req.ActorId, payrunEmployeeRepo);

        runRepo.Update(run);
        await uow.SaveChangesAsync(ct);
    }
}
