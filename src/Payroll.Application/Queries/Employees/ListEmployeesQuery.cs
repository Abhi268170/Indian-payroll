using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record ListEmployeesQuery : IRequest<IReadOnlyList<EmployeeDto>>;

internal sealed class ListEmployeesHandler(IEmployeeRepository employees)
    : IRequestHandler<ListEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public async Task<IReadOnlyList<EmployeeDto>> Handle(ListEmployeesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Employee> all = await employees.GetActiveByTenantAsync(cancellationToken);
        return all.Select(ToDto).ToList();
    }

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.FullName,
        e.DateOfBirth, e.Gender, e.DateOfJoining, e.EmploymentType,
        e.Status, e.WorkState, e.DepartmentId, e.DesignationId,
        e.BranchId, e.CostCentreId);
}
