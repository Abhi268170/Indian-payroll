using MediatR;
using Payroll.Application.DTOs;

namespace Payroll.Application.Queries.OrgStructure;

public record ListDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;
