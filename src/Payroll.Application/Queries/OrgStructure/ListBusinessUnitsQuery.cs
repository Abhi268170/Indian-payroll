using MediatR;
using Payroll.Application.DTOs;

namespace Payroll.Application.Queries.OrgStructure;

public record ListBusinessUnitsQuery : IRequest<IReadOnlyList<BusinessUnitDto>>;
