using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Platform.Tenants;

internal sealed class CreateTenantHandler(
    ITenantRepository repository,
    IPlatformUnitOfWork unitOfWork,
    ITenantSchemaProvisioner provisioner,
    IUserService userService,
    IEmailJobDispatcher emailJobDispatcher,
    IOptions<EmailOptions> emailOptions,
    ILogger<CreateTenantHandler> logger) : IRequestHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        Tenant? existing = await repository.GetBySlugAsync(command.Slug, cancellationToken);
        if (existing is not null)
            throw new DomainException($"Tenant with slug '{command.Slug}' already exists.");

        Tenant tenant = Tenant.Create(command.DisplayName, command.Slug);
        await repository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Deliberately not the request token: a client disconnect (e.g. proxy
            // timeout) must not abort schema migrations mid-flight.
            await provisioner.ProvisionAsync(tenant.Schema, tenant.Id, tenant.DisplayName, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema provisioning failed for tenant {TenantId}. Rolling back.", tenant.Id);
            try
            {
                // Rollback must run even when the request was aborted.
                await provisioner.DropAsync(tenant.Schema, CancellationToken.None);
            }
            catch (Exception dropEx)
            {
                logger.LogError(dropEx, "Failed to drop schema {Schema} during rollback.", tenant.Schema);
            }
            await repository.DeleteAsync(tenant, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw new InvalidOperationException("Failed to provision tenant schema.", ex);
        }

        await userService.CreateOrgAdminAsync(command.AdminEmail, tenant.Id, "OrgAdmin", cancellationToken);

        string token = await userService.GeneratePasswordResetTokenAsync(command.AdminEmail, cancellationToken);
        string setPasswordUrl = BuildSetPasswordUrl(emailOptions.Value.BaseUrl, token, command.AdminEmail, command.Slug);
        emailJobDispatcher.EnqueueWelcomeEmail(command.AdminEmail, command.Slug, setPasswordUrl);

        return tenant.Id;
    }

    private static string BuildSetPasswordUrl(string baseUrl, string token, string email, string slug)
        => $"{baseUrl}/set-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}&slug={Uri.EscapeDataString(slug)}";
}
