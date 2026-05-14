using Payroll.Domain.Enums;

namespace Payroll.Application.DTOs;

public sealed record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    Gender Gender,
    DateOnly DateOfJoining,
    EmploymentType EmploymentType,
    EmployeeStatus Status,
    IndianState WorkState,
    Guid DepartmentId,
    Guid DesignationId,
    Guid? BranchId,
    Guid? CostCentreId);
