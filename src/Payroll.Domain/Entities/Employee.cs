using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class Employee : AuditableEntity
{
    private Employee() { }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string EmployeeCode { get; private set; } = string.Empty;
    // Stored as AES-256 ciphertext — encryption handled by IEncryptionService in Infrastructure
    public string EncryptedPAN { get; private set; } = string.Empty;
    public string? EncryptedAadhaar { get; private set; }
    public string? EncryptedBankAccount { get; private set; }
    public string? EncryptedIFSC { get; private set; }
    public string? UAN { get; private set; }
    public string? ESICIPNumber { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public DateOnly DateOfJoining { get; private set; }
    public DateOnly? DateOfLeaving { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public IndianState WorkState { get; private set; }
    public bool PFOptOut { get; private set; }
    public bool IsPWD { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid DesignationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCentreId { get; private set; }

    public static Employee Create(
        string firstName,
        string lastName,
        string employeeCode,
        string encryptedPAN,
        Guid tenantId,
        Guid departmentId,
        Guid designationId,
        DateOnly dateOfJoining,
        IndianState workState,
        EmploymentType employmentType,
        Guid createdBy) => new()
        {
            FirstName = firstName,
            LastName = lastName,
            EmployeeCode = employeeCode,
            EncryptedPAN = encryptedPAN,
            TenantId = tenantId,
            DepartmentId = departmentId,
            DesignationId = designationId,
            DateOfJoining = dateOfJoining,
            WorkState = workState,
            EmploymentType = employmentType,
            Status = EmployeeStatus.Active,
            CreatedBy = createdBy
        };

    public string FullName => $"{FirstName} {LastName}";
}
