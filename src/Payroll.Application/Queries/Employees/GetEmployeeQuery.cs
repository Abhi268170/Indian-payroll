using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Employees;

public record GetEmployeeQuery(Guid Id) : IRequest<EmployeeDto>;

internal sealed class GetEmployeeHandler(IEmployeeRepository employees)
    : IRequestHandler<GetEmployeeQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        Employee employee = await employees.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Employee {request.Id} not found.");

        return new EmployeeDto(
            employee.Id, employee.EmployeeCode, employee.FirstName, employee.LastName, employee.FullName,
            employee.DateOfBirth, employee.Gender, employee.DateOfJoining, employee.EmploymentType,
            employee.Status, employee.WorkState, employee.DepartmentId, employee.DesignationId,
            employee.BranchId, employee.CostCentreId);
    }
}
