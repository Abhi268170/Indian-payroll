using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Auth;

internal sealed class ForgotPasswordCommandHandler(
    IUserService userService,
    IEmailJobDispatcher emailJobDispatcher,
    IOptions<EmailOptions> emailOptions,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        try
        {
            string token = await userService.GeneratePasswordResetTokenAsync(command.Email, cancellationToken);
            string resetUrl = $"{emailOptions.Value.BaseUrl}/set-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(command.Email)}";
            emailJobDispatcher.EnqueuePasswordResetEmail(command.Email, resetUrl);
        }
        catch (DomainException)
        {
            // User not found — swallow silently, no enumeration
            logger.LogDebug("Forgot-password requested for unknown email (suppressed).");
        }

        return Unit.Value;
    }
}
