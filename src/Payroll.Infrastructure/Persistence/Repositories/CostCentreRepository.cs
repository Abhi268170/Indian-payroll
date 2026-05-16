using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class CostCentreRepository(PayrollDbContext db) : ICostCentreRepository
{
    public async Task<IReadOnlyList<CostCentre>> ListAsync(CancellationToken ct = default) =>
        await db.CostCentres.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<CostCentre?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.CostCentres.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(CostCentre costCentre, CancellationToken ct = default) =>
        await db.CostCentres.AddAsync(costCentre, ct);

    public void Update(CostCentre costCentre) =>
        db.CostCentres.Update(costCentre);

    public void Remove(CostCentre costCentre) =>
        db.CostCentres.Remove(costCentre);
}
