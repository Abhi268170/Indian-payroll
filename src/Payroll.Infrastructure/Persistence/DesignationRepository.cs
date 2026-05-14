using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class DesignationRepository(PayrollDbContext db) : IDesignationRepository
{
    public Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Designations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Designation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Designations.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Designation designation, CancellationToken cancellationToken = default) =>
        await db.Designations.AddAsync(designation, cancellationToken);
}
