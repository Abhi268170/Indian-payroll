namespace Payroll.Domain.Interfaces;

public interface ITenantSchemaProvisioner
{
    Task ProvisionAsync(string schemaName, Guid tenantId, string displayName, CancellationToken cancellationToken = default);
    Task DropAsync(string schemaName, CancellationToken cancellationToken = default);
}
