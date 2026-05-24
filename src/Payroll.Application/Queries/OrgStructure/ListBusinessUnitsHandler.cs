using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public sealed class ListBusinessUnitsHandler(IBusinessUnitRepository repo)
    : IRequestHandler<ListBusinessUnitsQuery, IReadOnlyList<BusinessUnitDto>>
{
    public async Task<IReadOnlyList<BusinessUnitDto>> Handle(ListBusinessUnitsQuery request, CancellationToken cancellationToken)
    {
        var businessUnits = await repo.ListAsync(cancellationToken);
        return businessUnits.Select(b => new BusinessUnitDto(b.Id, b.Name, b.Description)).ToList();
    }
}
