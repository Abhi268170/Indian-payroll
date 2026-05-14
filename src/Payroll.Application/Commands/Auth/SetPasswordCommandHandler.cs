using MediatR;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Auth;

internal sealed class SetPasswordCommandHandler(IUserService userService) : IRequestHandler<SetPasswordCommand, Unit>
{
    public async Task<Unit> Handle(SetPasswordCommand command, CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(command.Email, command.Token, command.NewPassword, cancellationToken);
        return Unit.Value;
    }
}
