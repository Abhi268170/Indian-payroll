using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record CreateDeductionCommand(
    string Name,
    string NameInPayslip,
    string? Code,
    string DeductionFrequency,
    Guid ActorId
) : IRequest<Guid>;
