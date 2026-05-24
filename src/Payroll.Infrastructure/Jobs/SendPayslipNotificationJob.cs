namespace Payroll.Infrastructure.Jobs;

// Implemented in U12 (SendPayslipEmailCommand)
public sealed class SendPayslipNotificationJob
{
    public Task Execute(Guid payrollRunId) => Task.CompletedTask;
}
