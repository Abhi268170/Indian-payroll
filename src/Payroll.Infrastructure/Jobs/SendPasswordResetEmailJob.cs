using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Jobs;

public sealed class SendPasswordResetEmailJob(IEmailService emailService)
{
    public async Task ExecuteAsync(string toEmail, string resetUrl, CancellationToken ct)
    {
        string subject = "Reset Your Password — Indian Payroll";
        string body = $"""
            <p>You requested a password reset for your <strong>Indian Payroll</strong> account.</p>
            <p><a href="{resetUrl}">Reset your password</a></p>
            <p>This link expires in 1 hour. If you did not request this, ignore this email.</p>
            """;

        await emailService.SendAsync(toEmail, subject, body, ct);
    }
}
