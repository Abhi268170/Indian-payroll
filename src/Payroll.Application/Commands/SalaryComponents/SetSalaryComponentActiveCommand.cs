using MediatR;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed record SetSalaryComponentActiveCommand(
    Guid Id,
    bool IsActive,
    Guid ActorId
) : IRequest;
