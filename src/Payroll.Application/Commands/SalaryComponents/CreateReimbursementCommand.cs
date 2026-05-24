using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record CreateReimbursementCommand(
    string Name,
    string NameInPayslip,
    string? Code,
    string ReimbursementType,
    decimal Amount,
    string UnclaimedHandling,
    Guid ActorId
) : IRequest<Guid>;
