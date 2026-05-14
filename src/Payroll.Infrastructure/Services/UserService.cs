using Microsoft.AspNetCore.Identity;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure.Services;

internal sealed class UserService(
    UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<Guid> CreateTenantUserAsync(
        string email,
        string password,
        string role,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            TenantId = tenantId,
            IsSuperAdmin = false,
        };

        IdentityResult result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"User creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user.Id;
    }
}
