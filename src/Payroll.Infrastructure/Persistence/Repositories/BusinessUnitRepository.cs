using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class BusinessUnitRepository(PayrollDbContext db) : IBusinessUnitRepository
{
    public async Task<IReadOnlyList<BusinessUnit>> ListAsync(CancellationToken ct = default) =>
        await db.BusinessUnits.OrderBy(b => b.Name).ToListAsync(ct);

    public Task<BusinessUnit?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.BusinessUnits.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(BusinessUnit businessUnit, CancellationToken ct = default) =>
        await db.BusinessUnits.AddAsync(businessUnit, ct);

    public void Update(BusinessUnit businessUnit) =>
        db.BusinessUnits.Update(businessUnit);

    public void Remove(BusinessUnit businessUnit) =>
        db.BusinessUnits.Remove(businessUnit);
}
