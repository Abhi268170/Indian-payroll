using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public sealed record ConfigureStatutoryBonusCommand(
    bool Enabled,
    decimal BonusRate,
    string BonusMode,
    int? BonusPayoutMonth,
    Guid ActorId) : IRequest;

internal sealed class ConfigureStatutoryBonusHandler(
    IStatutoryConfigRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<ConfigureStatutoryBonusCommand>
{
    public async Task Handle(ConfigureStatutoryBonusCommand cmd, CancellationToken ct)
    {
        StatutoryOrgConfig? existing = await repo.GetByTenantAsync(ct);
        bool isNew = existing is null;
        StatutoryOrgConfig config = existing
            ?? StatutoryOrgConfig.CreateDefault(tenantContext.TenantId, cmd.ActorId);

        config.ConfigureStatutoryBonus(cmd.Enabled, cmd.BonusRate, cmd.BonusMode, cmd.BonusPayoutMonth, cmd.ActorId);

        if (isNew) await repo.AddAsync(config, ct);
        else repo.Update(config);

        await uow.SaveChangesAsync(ct);
    }
}
