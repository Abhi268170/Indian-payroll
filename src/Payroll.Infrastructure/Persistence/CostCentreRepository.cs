using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class CostCentreRepository(PayrollDbContext db) : ICostCentreRepository
{
    public Task<CostCentre?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CostCentres.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CostCentre>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.CostCentres.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(CostCentre costCentre, CancellationToken cancellationToken = default) =>
        await db.CostCentres.AddAsync(costCentre, cancellationToken);
}
