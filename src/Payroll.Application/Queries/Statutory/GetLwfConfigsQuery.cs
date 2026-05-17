using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Statutory;

public sealed record GetLwfConfigsQuery(IEnumerable<string> StateCodes) : IRequest<IReadOnlyList<LwfStateConfigDto>>;

internal sealed class GetLwfConfigsHandler(IStatutoryConfigRepository repo)
    : IRequestHandler<GetLwfConfigsQuery, IReadOnlyList<LwfStateConfigDto>>
{
    public async Task<IReadOnlyList<LwfStateConfigDto>> Handle(GetLwfConfigsQuery request, CancellationToken ct)
    {
        IReadOnlyList<LwfStateConfig> configs = await repo.GetLwfConfigsAsync(request.StateCodes, ct);
        return configs.Select(c => new LwfStateConfigDto(
            c.StateCode, c.EffectiveDate, c.EmployeeAmount, c.EmployerAmount,
            c.IsPercentageBased, c.EmployeeRate, c.EmployerRate,
            c.Frequency, c.DeductionMonth, c.WageThreshold)).ToList();
    }
}
