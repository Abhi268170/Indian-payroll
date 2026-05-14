namespace Payroll.Domain.Interfaces;

public interface IUserService
{
    Task<Guid> CreateTenantUserAsync(
        string email,
        string password,
        string role,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
