using System.Text.Json;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Commands.PayrollRuns;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using ImportRowError = Payroll.Application.DTOs.ImportRowError;
using Payroll.Application.Utilities;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)]
[Queue("payroll")]
public sealed class BulkImportBackgroundJob(
    ITenantContext tenantContext,
    PlatformDbContext platformDb,
    ISender sender,
    IJobProgressService jobProgress)
{
    public async Task Execute(Guid tenantId, string jobId, Guid runId, string csvContent, string importType, Guid actorId)
    {
        await SetupTenantContextAsync(tenantId);

        IReadOnlyList<string[]> allRows = CsvParser.Parse(csvContent);
        List<IReadOnlyList<string[]>> chunks = CsvParser.SplitIntoChunks(allRows).ToList();

        await jobProgress.InitializeAsync(tenantId, jobId, allRows.Count);

        int totalApplied = 0;
        List<ImportRowError> allErrors = [];
        int processed = 0;

        try
        {
            foreach (IReadOnlyList<string[]> chunk in chunks)
            {
                string chunkCsv = CsvParser.ReconstructCsv(chunk);

                ImportResult result = importType switch
                {
                    "lop" => await sender.Send(new BulkImportLopCommand(runId, chunkCsv, actorId)),
                    "earnings" => await sender.Send(new BulkImportOneTimeEarningsCommand(runId, chunkCsv, actorId)),
                    "reimbursements" => await sender.Send(new BulkImportReimbursementsCommand(runId, chunkCsv, actorId)),
                    _ => throw new InvalidOperationException($"Unknown import type: {importType}")
                };

                totalApplied += result.Applied;
                foreach (ImportRowError err in result.Errors) allErrors.Add(err);
                processed += chunk.Count;
                await jobProgress.UpdateAsync(tenantId, jobId, processed);
            }

            string resultJson = JsonSerializer.Serialize(new ImportResult(totalApplied, allErrors.AsReadOnly()));
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
