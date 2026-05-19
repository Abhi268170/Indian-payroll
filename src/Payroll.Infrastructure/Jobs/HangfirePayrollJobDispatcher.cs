using Hangfire;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Jobs;

internal sealed class HangfirePayrollJobDispatcher : IPayrollJobDispatcher
{
    public void EnqueueGeneratePayslips(Guid payrollRunId, Guid tenantId) =>
        BackgroundJob.Enqueue<GeneratePayslipsJob>(j => j.Execute(payrollRunId, tenantId));

    public void EnqueueGeneratePayslipsThenNotify(Guid payrollRunId, Guid tenantId)
    {
        string generateJobId = BackgroundJob.Enqueue<GeneratePayslipsJob>(j => j.Execute(payrollRunId, tenantId));
        BackgroundJob.ContinueJobWith<SendPayslipNotificationJob>(generateJobId, j => j.Execute(payrollRunId));
    }
}
