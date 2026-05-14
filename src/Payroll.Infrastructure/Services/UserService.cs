using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
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

    public async Task<Guid> CreateOrgAdminAsync(
        string email,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            TenantId = tenantId,
            IsSuperAdmin = false,
        };

        // Create without password — user sets it via set-password link
        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"User creation failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user.Id;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
            throw new DomainException($"User '{email}' not found.");

        string token = await userManager.GeneratePasswordResetTokenAsync(user);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public async Task ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
            throw new DomainException("Invalid email or token.");

        string decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        IdentityResult result = await userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new DomainException($"Password reset failed: {errors}");
        }
    }
}
