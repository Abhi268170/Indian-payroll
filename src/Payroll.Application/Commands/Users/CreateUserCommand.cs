using MediatR;

namespace Payroll.Application.Commands.Users;

public record CreateUserCommand(string Email, string Password, string Role, Guid ActorId) : IRequest<Guid>;
