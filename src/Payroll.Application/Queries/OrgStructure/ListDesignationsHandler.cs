using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public sealed class ListDesignationsHandler(IDesignationRepository repo)
    : IRequestHandler<ListDesignationsQuery, IReadOnlyList<DesignationDto>>
{
    public async Task<IReadOnlyList<DesignationDto>> Handle(ListDesignationsQuery request, CancellationToken cancellationToken)
    {
        var designations = await repo.ListAsync(cancellationToken);
        return designations.Select(d => new DesignationDto(d.Id, d.Name)).ToList();
    }
}
