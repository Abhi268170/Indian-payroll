using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class PlatformUnitOfWork(PlatformDbContext db) : IPlatformUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
