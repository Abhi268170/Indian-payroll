namespace Payroll.Domain.Interfaces;

public interface ITenantSchemaProvisioner
{
    Task ProvisionAsync(string schemaName, Guid tenantId, CancellationToken cancellationToken = default);
    Task DropAsync(string schemaName, CancellationToken cancellationToken = default);
}
