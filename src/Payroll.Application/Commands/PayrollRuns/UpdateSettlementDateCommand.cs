using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record UpdateSettlementDateCommand(Guid RunId, DateOnly SettlementDate, Guid ActorId) : IRequest;

public sealed class UpdateSettlementDateCommandValidator : AbstractValidator<UpdateSettlementDateCommand>
{
    public UpdateSettlementDateCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class UpdateSettlementDateHandler(
    IPayrollRunRepository runRepo,
    IEmployeeExitRepository exitRepo,
    IUnitOfWork uow)
    : IRequestHandler<UpdateSettlementDateCommand>
{
    public async Task Handle(UpdateSettlementDateCommand req, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        run.UpdateSettlementDate(req.SettlementDate, req.ActorId);
        runRepo.Update(run);

        // Keep the linked exit's settlement date in sync for single FinalSettlement runs.
        var exits = await exitRepo.GetByFnfRunIdsAsync(new[] { req.RunId }, ct);
        foreach (EmployeeExit exit in exits)
        {
            exit.Update(exit.LastWorkingDay, exit.Reason, exit.SettlementMode,
                req.SettlementDate, exit.PersonalEmail, exit.Notes, req.ActorId);
            exitRepo.Update(exit);
        }

        await uow.SaveChangesAsync(ct);
    }
}
