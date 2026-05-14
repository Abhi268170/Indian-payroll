using MediatR;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.OrgStructure;

public record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string EmployeeCode,
    string PAN,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly DateOfJoining,
    IndianState WorkState,
    EmploymentType EmploymentType,
    Guid DepartmentId,
    Guid DesignationId,
    Guid ActorId,
    Guid? BranchId = null,
    Guid? CostCentreId = null) : IRequest<Guid>;
