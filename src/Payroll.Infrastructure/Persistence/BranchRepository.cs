using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class BranchRepository(PayrollDbContext db) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Branches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Branches.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Branch branch, CancellationToken cancellationToken = default) =>
        await db.Branches.AddAsync(branch, cancellationToken);
}
