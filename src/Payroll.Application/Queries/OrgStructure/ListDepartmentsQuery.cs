using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.OrgStructure;

public record ListDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

internal sealed class ListDepartmentsHandler(IDepartmentRepository departments)
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    public async Task<IReadOnlyList<DepartmentDto>> Handle(ListDepartmentsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Department> all = await departments.GetAllAsync(cancellationToken);
        return all.Select(d => new DepartmentDto(d.Id, d.Name, d.Code)).ToList();
    }
}
