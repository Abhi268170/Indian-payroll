using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public record UpsertPtRegistrationCommand(string StateCode, string RegistrationNumber, Guid ActorId) : IRequest;

internal sealed class UpsertPtRegistrationHandler(
    IStatutoryConfigRepository repo,
    IUnitOfWork uow)
    : IRequestHandler<UpsertPtRegistrationCommand>
{
    public async Task Handle(UpsertPtRegistrationCommand req, CancellationToken ct)
    {
        var existing = await repo.GetPtRegistrationAsync(req.StateCode, ct);
        if (existing is null)
        {
            var registration = PtStateRegistration.Create(req.StateCode, req.RegistrationNumber, req.ActorId);
            await repo.AddPtRegistrationAsync(registration, ct);
        }
        else
        {
            existing.UpdateRegistrationNumber(req.RegistrationNumber, req.ActorId);
            repo.UpdatePtRegistration(existing);
        }

        await uow.SaveChangesAsync(ct);
    }
}
