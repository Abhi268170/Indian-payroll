namespace Payroll.Domain.Interfaces;

public interface IPlatformUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
