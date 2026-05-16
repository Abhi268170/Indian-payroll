using MediatR;
using Payroll.Application.DTOs;

namespace Payroll.Application.Queries.OrgStructure;

public record ListDesignationsQuery : IRequest<IReadOnlyList<DesignationDto>>;
