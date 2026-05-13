using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class UnitOfWork(PayrollDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
