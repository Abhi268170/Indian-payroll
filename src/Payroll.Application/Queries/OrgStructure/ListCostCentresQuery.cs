using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public record ListCostCentresQuery : IRequest<IReadOnlyList<CostCentreDto>>;

internal sealed class ListCostCentresHandler(ICostCentreRepository costCentres)
    : IRequestHandler<ListCostCentresQuery, IReadOnlyList<CostCentreDto>>
{
    public async Task<IReadOnlyList<CostCentreDto>> Handle(ListCostCentresQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<CostCentre> all = await costCentres.GetAllAsync(cancellationToken);
        return all.Select(c => new CostCentreDto(c.Id, c.Name, c.Code)).ToList();
    }
}
