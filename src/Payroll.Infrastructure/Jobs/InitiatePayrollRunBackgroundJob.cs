using System.Text.Json;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)]
[Queue("payroll")]
public sealed class InitiatePayrollRunBackgroundJob(
    ITenantContext tenantContext,
    PlatformDbContext platformDb,
    ISender sender,
    IJobProgressService jobProgress)
{
    public async Task Execute(Guid tenantId, string jobId, Guid actorId)
    {
        await SetupTenantContextAsync(tenantId);
        await jobProgress.InitializeAsync(tenantId, jobId, total: 1);

        try
        {
            PayrollRunSummaryDto dto = await sender.Send(new InitiatePayrollRunCommand(actorId));
            // Use Web defaults so the camelCase keys match what the API exposes elsewhere;
            // the frontend reads dto.id from this payload.
            string resultJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await jobProgress.UpdateAsync(tenantId, jobId, processed: 1);
            await jobProgress.CompleteAsync(tenantId, jobId, resultJson);
        }
        catch (Exception ex)
        {
            await jobProgress.FailAsync(tenantId, jobId, ex.Message);
            throw;
        }
    }

    private async Task SetupTenantContextAsync(Guid tenantId)
    {
        Domain.Entities.Tenant tenant = await platformDb.Tenants.FirstAsync(t => t.Id == tenantId);
        tenantContext.SetTenant(new TenantInfo(tenant.Id, tenant.Schema, tenant.Slug, tenant.IsActive));
    }
}
