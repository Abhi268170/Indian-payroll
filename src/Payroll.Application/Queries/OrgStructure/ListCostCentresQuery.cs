using MediatR;
using Payroll.Application.DTOs;

namespace Payroll.Application.Queries.OrgStructure;

public record ListCostCentresQuery : IRequest<IReadOnlyList<CostCentreDto>>;
