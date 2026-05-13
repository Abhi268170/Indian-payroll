namespace Payroll.Domain.Interfaces;

public interface ITokenRevocationService
{
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
