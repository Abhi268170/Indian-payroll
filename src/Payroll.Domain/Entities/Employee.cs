using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class Employee : AuditableEntity
{
    private Employee() { }

    // Basic
    public string FirstName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public string EmployeeCode { get; private set; } = string.Empty;
    public string WorkEmail { get; private set; } = string.Empty;
    public string? MobileNumber { get; private set; }
    public Gender Gender { get; private set; }
    public DateOnly DateOfJoining { get; private set; }
    public DateOnly? DateOfLeaving { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public bool IsDirector { get; private set; }
    public bool EnablePortalAccess { get; private set; }
    public bool ProfileComplete { get; private set; }
    public Guid TenantId { get; private set; }

    // Org structure
    public Guid DepartmentId { get; private set; }
    public Guid DesignationId { get; private set; }
    public Guid WorkLocationId { get; private set; }
    public Guid? BusinessUnitId { get; private set; }

    // Personal details
    public DateOnly DateOfBirth { get; private set; }
    public string? FathersName { get; private set; }
    public string? PersonalEmail { get; private set; }
    public DifferentlyAbledType DifferentlyAbledType { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public IndianState? ResidentialState { get; private set; }
    public string? PinCode { get; private set; }

    // Payment
    public PaymentMode PaymentMode { get; private set; }
    public string? AccountHolderName { get; private set; }
    public string? BankName { get; private set; }
    public AccountType? AccountType { get; private set; }
    // Stored as AES-256 ciphertext — encryption handled by IEncryptionService in Infrastructure
    public string? EncryptedPAN { get; private set; }
    public string? EncryptedAadhaar { get; private set; }
    public string? EncryptedBankAccount { get; private set; }
    public string? EncryptedIFSC { get; private set; }

    // Statutory
    public string? UAN { get; private set; }
    public string? ESICIPNumber { get; private set; }
    public bool EpfEnabled { get; private set; } = true;
    public bool EsiEnabled { get; private set; } = true;
    public bool PtEnabled { get; private set; } = true;
    public bool LwfEnabled { get; private set; } = true;
    public bool IsPWD { get; private set; }

    public string FullName => MiddleName is null
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";

    public static Employee CreateStep1(
        string firstName,
        string? middleName,
        string lastName,
        string employeeCode,
        string workEmail,
        string? mobileNumber,
        Gender gender,
        DateOnly dateOfJoining,
        EmploymentType employmentType,
        bool isDirector,
        bool enablePortalAccess,
        Guid tenantId,
        Guid departmentId,
        Guid designationId,
        Guid workLocationId,
        Guid? businessUnitId,
        DateOnly dateOfBirth,
        Guid createdBy) => new()
        {
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            EmployeeCode = employeeCode,
            WorkEmail = workEmail,
            MobileNumber = mobileNumber,
            Gender = gender,
            DateOfJoining = dateOfJoining,
            EmploymentType = employmentType,
            IsDirector = isDirector,
            EnablePortalAccess = enablePortalAccess,
            Status = EmployeeStatus.Active,
            ProfileComplete = false,
            TenantId = tenantId,
            DepartmentId = departmentId,
            DesignationId = designationId,
            WorkLocationId = workLocationId,
            BusinessUnitId = businessUnitId,
            DateOfBirth = dateOfBirth,
            DifferentlyAbledType = DifferentlyAbledType.None,
            PaymentMode = PaymentMode.BankTransfer,
            CreatedBy = createdBy
        };

    public void UpdateBasicDetails(
        string firstName,
        string? middleName,
        string lastName,
        string? mobileNumber,
        Gender gender,
        bool isDirector,
        bool enablePortalAccess,
        Guid departmentId,
        Guid designationId,
        Guid workLocationId,
        Guid? businessUnitId,
        Guid updatedBy)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        MobileNumber = mobileNumber;
        Gender = gender;
        IsDirector = isDirector;
        EnablePortalAccess = enablePortalAccess;
        DepartmentId = departmentId;
        DesignationId = designationId;
        WorkLocationId = workLocationId;
        BusinessUnitId = businessUnitId;
        SetUpdated(updatedBy);
    }

    public void UpdatePersonalDetails(
        DateOnly dateOfBirth,
        string? fathersName,
        string? encryptedPAN,
        string? encryptedAadhaar,
        string? personalEmail,
        DifferentlyAbledType differentlyAbledType,
        string? addressLine1,
        string? addressLine2,
        string? city,
        IndianState? residentialState,
        string? pinCode,
        Guid updatedBy)
    {
        DateOfBirth = dateOfBirth;
        FathersName = fathersName;
        EncryptedPAN = encryptedPAN;
        if (encryptedAadhaar is not null)
            EncryptedAadhaar = encryptedAadhaar;
        PersonalEmail = personalEmail;
        DifferentlyAbledType = differentlyAbledType;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        ResidentialState = residentialState;
        PinCode = pinCode;
        SetUpdated(updatedBy);
    }

    public void UpdatePaymentInfo(
        PaymentMode paymentMode,
        string? accountHolderName,
        string? bankName,
        AccountType? accountType,
        string? encryptedBankAccount,
        string? encryptedIFSC,
        Guid updatedBy)
    {
        PaymentMode = paymentMode;
        AccountHolderName = accountHolderName;
        BankName = bankName;
        AccountType = accountType;
        EncryptedBankAccount = encryptedBankAccount;
        EncryptedIFSC = encryptedIFSC;
        SetUpdated(updatedBy);
    }

    public void UpdateStatutoryDetails(
        bool epfEnabled,
        bool esiEnabled,
        bool ptEnabled,
        bool lwfEnabled,
        string? uan,
        string? esicipNumber,
        Guid updatedBy)
    {
        EpfEnabled = epfEnabled;
        EsiEnabled = esiEnabled;
        PtEnabled = ptEnabled;
        LwfEnabled = lwfEnabled;
        UAN = uan;
        ESICIPNumber = esicipNumber;
        SetUpdated(updatedBy);
    }

    public void MarkProfileComplete(Guid updatedBy)
    {
        ProfileComplete = true;
        SetUpdated(updatedBy);
    }

    public void MarkExited(DateOnly lastWorkingDay, Guid updatedBy)
    {
        Status = EmployeeStatus.Exited;
        DateOfLeaving = lastWorkingDay;
        SetUpdated(updatedBy);
    }

    public void RevertExit(Guid updatedBy)
    {
        Status = EmployeeStatus.Active;
        DateOfLeaving = null;
        SetUpdated(updatedBy);
    }
}
