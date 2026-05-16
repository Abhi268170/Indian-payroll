using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public sealed class ListDepartmentsHandler(IDepartmentRepository repo)
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(ListDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await repo.ListAsync(cancellationToken);
        return departments.Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.Description)).ToList();
    }
}
