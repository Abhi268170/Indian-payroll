using FluentValidation;
using MediatR;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Statutory;

public sealed record ConfigureEpfCommand(
    bool Enabled,
    string? EstablishmentCode,
    string EmployeeContributionRate,
    string EmployerContributionRate,
    bool IncludeEmployerInCtc,
    bool IncludeEdliInCtc,
    bool IncludeAdminInCtc,
    bool OverrideAtEmployeeLevel,
    bool ProRateRestrictedPfWage,
    bool ConsiderSalaryOnLop,
    Guid ActorId) : IRequest;

public sealed class ConfigureEpfCommandValidator : AbstractValidator<ConfigureEpfCommand>
{
    private static readonly HashSet<string> ValidRates =
    [
        "ActualPfWage12", "RestrictedWage12", "Gross12"
    ];

    public ConfigureEpfCommandValidator()
    {
        RuleFor(c => c.EstablishmentCode)
            .MaximumLength(30)
            .Matches(@"^[A-Z]{2}/[A-Z]{3}/\d{7}/[A-Z0-9]{3}$")
            .When(c => !string.IsNullOrEmpty(c.EstablishmentCode))
            .WithMessage("EPF establishment code must be in format AA/AAA/0000000/XXX.");

        RuleFor(c => c.EmployeeContributionRate)
            .Must(r => ValidRates.Contains(r))
            .WithMessage("Invalid employee contribution rate.");

        RuleFor(c => c.EmployerContributionRate)
            .Must(r => ValidRates.Contains(r))
            .WithMessage("Invalid employer contribution rate.");
    }
}

internal sealed class ConfigureEpfHandler(
    IStatutoryConfigRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow) : IRequestHandler<ConfigureEpfCommand>
{
    public async Task Handle(ConfigureEpfCommand cmd, CancellationToken ct)
    {
        StatutoryOrgConfig? existing = await repo.GetByTenantAsync(ct);
        bool isNew = existing is null;
        StatutoryOrgConfig config = existing
            ?? StatutoryOrgConfig.CreateDefault(tenantContext.TenantId, cmd.ActorId);

        config.ConfigureEpf(
            cmd.Enabled,
            cmd.EstablishmentCode,
            cmd.EmployeeContributionRate,
            cmd.EmployerContributionRate,
            cmd.IncludeEmployerInCtc,
            cmd.IncludeEdliInCtc,
            cmd.IncludeAdminInCtc,
            cmd.OverrideAtEmployeeLevel,
            cmd.ProRateRestrictedPfWage,
            cmd.ConsiderSalaryOnLop,
            cmd.ActorId);

        if (isNew)
            await repo.AddAsync(config, ct);
        else
            repo.Update(config);

        await uow.SaveChangesAsync(ct);
    }
}
