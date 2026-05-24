using FluentValidation;
using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public sealed record ConfigureEsiCommand(
    bool Enabled,
    string? EstablishmentCode,
    bool NotifiedArea,
    Guid ActorId) : IRequest;

public sealed class ConfigureEsiCommandValidator : AbstractValidator<ConfigureEsiCommand>
{
    public ConfigureEsiCommandValidator()
    {
        RuleFor(c => c.EstablishmentCode)
            .MaximumLength(30)
            .When(c => !string.IsNullOrEmpty(c.EstablishmentCode));
    }
}

internal sealed class ConfigureEsiHandler(
    IStatutoryConfigRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<ConfigureEsiCommand>
{
    public async Task Handle(ConfigureEsiCommand cmd, CancellationToken ct)
    {
        StatutoryOrgConfig? existing = await repo.GetByTenantAsync(ct);
        bool isNew = existing is null;
        StatutoryOrgConfig config = existing
            ?? StatutoryOrgConfig.CreateDefault(tenantContext.TenantId, cmd.ActorId);

        config.ConfigureEsi(cmd.Enabled, cmd.EstablishmentCode, cmd.NotifiedArea, cmd.ActorId);

        if (isNew) await repo.AddAsync(config, ct);
        else repo.Update(config);

        await uow.SaveChangesAsync(ct);
    }
}
