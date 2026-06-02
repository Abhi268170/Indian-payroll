using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.Interfaces;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Jobs;

// WI-18: emails the published payslips for a run. Chained after GeneratePayslipsJob
// via EnqueueGeneratePayslipsThenNotify. Per-payslip failures are isolated so one
// bad recipient doesn't block the rest; the job itself retries on transient faults.
[AutomaticRetry(Attempts = 3)]
[Queue("notifications")]
public sealed class SendPayslipNotificationJob(
    ITenantContext tenantContext,
    PlatformDbContext platformDb,
    IPayslipRepository payslipRepo,
    ISender sender)
{
    public async Task Execute(Guid payrollRunId, Guid tenantId)
    {
        Tenant tenant = await platformDb.Tenants.FirstAsync(t => t.Id == tenantId);
        tenantContext.SetTenant(new TenantInfo(tenant.Id, tenant.Schema, tenant.Slug, tenant.IsActive));

        var payslips = await payslipRepo.GetByRunIdAsync(payrollRunId);
        foreach (Payslip payslip in payslips)
        {
            if (!payslip.IsPublished) continue;
            await sender.Send(new SendPayslipEmailCommand(payrollRunId, payslip.EmployeeId));
        }
    }
}
