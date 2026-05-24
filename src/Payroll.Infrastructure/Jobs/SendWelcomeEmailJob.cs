using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Jobs;

public sealed class SendWelcomeEmailJob(IEmailService emailService)
{
    public async Task ExecuteAsync(string toEmail, string orgSlug, string setPasswordUrl, CancellationToken ct)
    {
        string subject = "Welcome to Indian Payroll — Set Your Password";
        string body = $"""
            <p>Welcome to <strong>Indian Payroll</strong>!</p>
            <p>Your organisation <strong>{orgSlug}</strong> has been provisioned.</p>
            <p>Set your password to get started:</p>
            <p><a href="{setPasswordUrl}">{setPasswordUrl}</a></p>
            <p>This link expires in 72 hours.</p>
            """;

        await emailService.SendAsync(toEmail, subject, body, ct);
    }
}
