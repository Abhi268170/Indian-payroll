using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Jobs;

public sealed class GeneratePayslipsJob(
    ITenantContext tenantContext,
    PlatformDbContext platformDb,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    ISender sender,
    IPayslipPdfGenerator pdfGenerator,
    IFileStorageService fileStorage,
    IPayslipRepository payslipRepo,
    IUnitOfWork uow)
{
    public async Task Execute(Guid payrollRunId, Guid tenantId)
    {
        await SetupTenantContextAsync(tenantId);

        var activeEmployees = await payrunEmployeeRepo.GetByRunIdWithStatusAsync(payrollRunId, PayrunEmployeeStatus.Active);

        foreach (PayrunEmployee pe in activeEmployees)
        {
            PayslipData data = await sender.Send(new GetPayslipDataQuery(payrollRunId, pe.EmployeeId));
            byte[] pdf = pdfGenerator.Generate(data);
            string storageKey = $"payslips/{tenantId}/{payrollRunId}/{pe.EmployeeId}.pdf";

            using MemoryStream stream = new(pdf);
            await fileStorage.UploadAsync(storageKey, stream, "application/pdf");

            Payslip? existing = await payslipRepo.GetByRunAndEmployeeAsync(payrollRunId, pe.EmployeeId);
            if (existing is not null)
            {
                existing.Publish(Guid.Empty);
                payslipRepo.Update(existing);
            }
            else
            {
                Payslip payslip = Payslip.Create(
                    payrollRunId,
                    pe.EmployeeId,
                    tenantId,
                    storageKey,
                    pe.NetPay,
                    data.NetPayInWords,
                    ytdDataJson: null,
                    createdBy: Guid.Empty);
                payslip.Publish(Guid.Empty);
                await payslipRepo.AddAsync(payslip);
            }
        }

        await uow.SaveChangesAsync();
    }

    private async Task SetupTenantContextAsync(Guid tenantId)
    {
        Domain.Entities.Tenant tenant = await platformDb.Tenants
            .FirstAsync(t => t.Id == tenantId);
        tenantContext.SetTenant(new TenantInfo(tenant.Id, tenant.Schema, tenant.Slug, tenant.IsActive));
    }
}
