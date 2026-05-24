using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public record ToggleLwfStateCommand(string StateCode, bool Enable, Guid ActorId) : IRequest;

internal sealed class ToggleLwfStateHandler(
    IStatutoryConfigRepository repo,
    IUnitOfWork uow)
    : IRequestHandler<ToggleLwfStateCommand>
{
    public async Task Handle(ToggleLwfStateCommand req, CancellationToken ct)
    {
        var config = await repo.GetLwfConfigAsync(req.StateCode, ct)
            ?? throw new NotFoundException($"LWF configuration for state {req.StateCode} not found.");

        if (req.Enable)
            config.Activate(req.ActorId);
        else
            config.Deactivate(req.ActorId);

        repo.UpdateLwfConfig(config);
        await uow.SaveChangesAsync(ct);
    }
}
