namespace Payroll.Domain.Interfaces;

public interface ITenantSchemaProvisioner
{
    Task ProvisionAsync(string schemaName, CancellationToken cancellationToken = default);
    Task DropAsync(string schemaName, CancellationToken cancellationToken = default);
}
