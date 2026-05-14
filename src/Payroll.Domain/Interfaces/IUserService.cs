namespace Payroll.Domain.Interfaces;

public interface IUserService
{
    Task<Guid> CreateTenantUserAsync(
        string email,
        string password,
        string role,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateOrgAdminAsync(
        string email,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
