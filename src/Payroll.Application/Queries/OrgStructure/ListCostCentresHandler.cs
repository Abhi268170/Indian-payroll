using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public sealed class ListCostCentresHandler(ICostCentreRepository repo)
    : IRequestHandler<ListCostCentresQuery, IReadOnlyList<CostCentreDto>>
{
    public async Task<IReadOnlyList<CostCentreDto>> Handle(ListCostCentresQuery request, CancellationToken cancellationToken)
    {
        var costCentres = await repo.ListAsync(cancellationToken);
        return costCentres.Select(c => new CostCentreDto(c.Id, c.Name, c.Code)).ToList();
    }
}
