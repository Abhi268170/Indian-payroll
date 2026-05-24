namespace Payroll.Application.Interfaces;

public interface IEmailJobDispatcher
{
    void EnqueueWelcomeEmail(string toEmail, string orgSlug, string setPasswordUrl);
    void EnqueuePasswordResetEmail(string toEmail, string resetUrl);
}
