using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public record ListDesignationsQuery : IRequest<IReadOnlyList<DesignationDto>>;

internal sealed class ListDesignationsHandler(IDesignationRepository designations)
    : IRequestHandler<ListDesignationsQuery, IReadOnlyList<DesignationDto>>
{
    public async Task<IReadOnlyList<DesignationDto>> Handle(ListDesignationsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Designation> all = await designations.GetAllAsync(cancellationToken);
        return all.Select(d => new DesignationDto(d.Id, d.Name)).ToList();
    }
}
