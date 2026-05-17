using Hangfire;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Jobs;

internal sealed class HangfirePayrollJobDispatcher : IPayrollJobDispatcher
{
    public void EnqueueGeneratePayslips(Guid payrollRunId) =>
        BackgroundJob.Enqueue<GeneratePayslipsJob>(j => j.Execute(payrollRunId));

    public void EnqueueGeneratePayslipsThenNotify(Guid payrollRunId)
    {
        string generateJobId = BackgroundJob.Enqueue<GeneratePayslipsJob>(j => j.Execute(payrollRunId));
        BackgroundJob.ContinueJobWith<SendPayslipNotificationJob>(generateJobId, j => j.Execute(payrollRunId));
    }
}
