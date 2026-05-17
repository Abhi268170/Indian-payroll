using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record CreateCorrectionCommand(
    string CorrectionName,
    string? Code,
    Guid ForCorrectionOfComponentId,
    Guid ActorId
) : IRequest<Guid>;
