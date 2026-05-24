using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class SalaryStructureTemplateRepository(PayrollDbContext db)
    : ISalaryStructureTemplateRepository
{
    public Task<SalaryStructureTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.SalaryStructureTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<SalaryStructureTemplate?> GetByIdWithComponentsAsync(Guid id, CancellationToken ct = default) =>
        db.SalaryStructureTemplates
          .Include(t => t.Components)
              .ThenInclude(c => c.Component)
          .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<SalaryStructureTemplate>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.SalaryStructureTemplates
          .Include(t => t.Components)
          .Where(t => t.TenantId == tenantId)
          .OrderBy(t => t.Name)
          .ToListAsync(ct);

    public async Task AddAsync(SalaryStructureTemplate template, CancellationToken ct = default) =>
        await db.SalaryStructureTemplates.AddAsync(template, ct);

    public async Task ReplaceComponentsAsync(Guid templateId, IEnumerable<SalaryStructureComponent> newComponents, CancellationToken ct = default)
    {
        await db.SalaryStructureComponents
            .Where(c => c.TemplateId == templateId)
            .ExecuteDeleteAsync(ct);
        await db.SalaryStructureComponents.AddRangeAsync(newComponents, ct);
    }
}
