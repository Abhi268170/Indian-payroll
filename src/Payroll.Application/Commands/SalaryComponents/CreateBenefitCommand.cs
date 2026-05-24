using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record CreateBenefitCommand(
    string Name,
    string NameInPayslip,
    string? Code,
    string BenefitType,
    decimal? BenefitPercentage,
    bool IsApplicableToAllEmployees,
    bool? IsNpsGovernmentSector,
    Guid ActorId
) : IRequest<Guid>;
