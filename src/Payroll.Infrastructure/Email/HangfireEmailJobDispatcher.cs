using Hangfire;
using Payroll.Application.Interfaces;
using Payroll.Infrastructure.Jobs;

namespace Payroll.Infrastructure.Email;

internal sealed class HangfireEmailJobDispatcher : IEmailJobDispatcher
{
    public void EnqueueWelcomeEmail(string toEmail, string orgSlug, string setPasswordUrl)
        => BackgroundJob.Enqueue<SendWelcomeEmailJob>(
            job => job.ExecuteAsync(toEmail, orgSlug, setPasswordUrl, CancellationToken.None));

    public void EnqueuePasswordResetEmail(string toEmail, string resetUrl)
        => BackgroundJob.Enqueue<SendPasswordResetEmailJob>(
            job => job.ExecuteAsync(toEmail, resetUrl, CancellationToken.None));
}
