using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class DesignationRepository(PayrollDbContext db) : IDesignationRepository
{
    public async Task<IReadOnlyList<Designation>> ListAsync(CancellationToken ct = default) =>
        await db.Designations.OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Designation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Designation designation, CancellationToken ct = default) =>
        await db.Designations.AddAsync(designation, ct);

    public void Update(Designation designation) =>
        db.Designations.Update(designation);

    public void Remove(Designation designation) =>
        db.Designations.Remove(designation);
}
