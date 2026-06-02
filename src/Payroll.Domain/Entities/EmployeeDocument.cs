namespace Payroll.Domain.Entities;

// WI-31: a generated HR document (e.g. relieving letter) stored in object
// storage and linked to an employee for retrieval.
public sealed class EmployeeDocument
{
    private EmployeeDocument() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EmployeeId { get; private set; }
    public Guid TenantId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid CreatedBy { get; private set; }

    public static EmployeeDocument Create(
        Guid employeeId, Guid tenantId, string documentType,
        string fileName, string storageKey, Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            TenantId = tenantId,
            DocumentType = documentType,
            FileName = fileName,
            StorageKey = storageKey,
            CreatedBy = createdBy,
        };
}
