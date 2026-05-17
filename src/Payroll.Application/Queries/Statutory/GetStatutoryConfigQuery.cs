using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Statutory;

public sealed record GetStatutoryConfigQuery : IRequest<StatutoryConfigDto?>;

internal sealed class GetStatutoryConfigHandler(IStatutoryConfigRepository repo)
    : IRequestHandler<GetStatutoryConfigQuery, StatutoryConfigDto?>
{
    public async Task<StatutoryConfigDto?> Handle(GetStatutoryConfigQuery _, CancellationToken ct)
    {
        StatutoryOrgConfig? config = await repo.GetByTenantAsync(ct);
        if (config is null) return null;

        return new StatutoryConfigDto(
            config.EpfEnabled,
            config.EpfEstablishmentCode,
            config.EpfEmployeeContributionRate,
            config.EpfEmployerContributionRate,
            config.EpfIncludeEmployerInCtc,
            config.EpfIncludeEdliInCtc,
            config.EpfIncludeAdminInCtc,
            config.EpfOverrideAtEmployeeLevel,
            config.EpfProRateRestrictedPfWage,
            config.EpfConsiderSalaryOnLop,
            config.EsiEnabled,
            config.EsiEstablishmentCode,
            config.EsiNotifiedArea,
            config.StatutoryBonusEnabled,
            config.BonusRate,
            config.BonusMode,
            config.BonusPayoutMonth);
    }
}
