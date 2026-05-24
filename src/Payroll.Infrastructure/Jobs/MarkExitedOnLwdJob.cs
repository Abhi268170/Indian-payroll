using Hangfire;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Interfaces;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Jobs;

// Daily sweep: flip Employee.Status to Exited the day after their LWD passes.
// Idempotent — runs once a day per tenant. The regular-run filter already excludes
// exiting employees via DateOfLeaving, so this job is purely cosmetic (drives the
// "Exited" badge in the Employees list); skipping or double-running is safe.
[AutomaticRetry(Attempts = 0)]
[Queue("payroll")]
public sealed class MarkExitedOnLwdJob(
    ITenantContext tenantContext,
    PlatformDbContext platformDb,
    IEmployeeRepository employeeRepo,
    IUnitOfWork uow)
{
    public async Task Execute()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var tenants = await platformDb.Tenants.Where(t => t.IsActive).ToListAsync();

        foreach (var tenant in tenants)
        {
            tenantContext.SetTenant(new TenantInfo(tenant.Id, tenant.Schema, tenant.Slug, tenant.IsActive));

            var due = (await employeeRepo.ListAsync(CancellationToken.None))
                .Where(e => e.Status == EmployeeStatus.Active
                    && e.DateOfLeaving != null
                    && e.DateOfLeaving < today)
                .ToList();

            if (due.Count == 0) continue;

            foreach (var emp in due)
                emp.MarkExited(emp.DateOfLeaving!.Value, updatedBy: Guid.Empty);

            await uow.SaveChangesAsync(CancellationToken.None);
        }
    }
}
