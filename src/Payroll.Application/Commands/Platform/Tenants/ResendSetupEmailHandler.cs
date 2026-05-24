using MediatR;
using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

internal sealed class ResendSetupEmailHandler(
    ITenantRepository repository,
    IUserService userService,
    IEmailJobDispatcher emailJobDispatcher,
    IOptions<EmailOptions> emailOptions) : IRequestHandler<ResendSetupEmailCommand>
{
    public async Task Handle(ResendSetupEmailCommand command, CancellationToken cancellationToken)
    {
        Tenant? tenant = await repository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{command.TenantId}' not found.");

        if (!tenant.IsActive)
            throw new DomainException("Tenant is suspended.");

        string? adminEmail = await userService.GetOrgAdminEmailAsync(tenant.Id, cancellationToken);
        if (adminEmail is null)
            throw new NotFoundException("Org admin account not found for this tenant.");

        string token;
        try
        {
            token = await userService.GeneratePasswordResetTokenAsync(adminEmail, cancellationToken);
        }
        catch (DomainException)
        {
            throw new NotFoundException("Org admin account not found for this tenant.");
        }

        string setPasswordUrl = BuildSetPasswordUrl(emailOptions.Value.BaseUrl, token, adminEmail, tenant.Slug);
        emailJobDispatcher.EnqueueWelcomeEmail(adminEmail, tenant.Slug, setPasswordUrl);
    }

    private static string BuildSetPasswordUrl(string baseUrl, string token, string email, string slug)
        => $"{baseUrl}/set-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}&slug={Uri.EscapeDataString(slug)}";
}
