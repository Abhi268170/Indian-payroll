using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record DeletePayrollRunCommand(Guid RunId, Guid ActorId) : IRequest;

public sealed class DeletePayrollRunHandler(
    IPayrollRunRepository runRepo,
    IUnitOfWork uow)
    : IRequestHandler<DeletePayrollRunCommand>
{
    public async Task Handle(DeletePayrollRunCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Only a Draft payroll run can be deleted.");

        run.Delete(req.ActorId);
        runRepo.Update(run);

        await uow.SaveChangesAsync(ct);
    }
}
