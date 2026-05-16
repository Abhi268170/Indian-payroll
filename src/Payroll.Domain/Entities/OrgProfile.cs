using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class OrgProfile : AuditableEntity
{
    public string CompanyName { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? Pan { get; private set; }
    public string? Gstin { get; private set; }
    public string? Website { get; private set; }
    public string? Industry { get; private set; }
    public DateOnly? IncorporationDate { get; private set; }

    // Registered address
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public IndianState? State { get; private set; }
    public string? PinCode { get; private set; }

    // Filing address — references a work location (nullable: new tenants have none)
    public Guid? FilingAddressWorkLocationId { get; private set; }

    // MinIO object key; null means no logo uploaded
    public string? LogoObjectKey { get; private set; }

    private OrgProfile() { }  // EF constructor

    public static OrgProfile Create(string companyName, Guid createdBy)
    {
        return new OrgProfile
        {
            CompanyName = companyName,
            CreatedBy = createdBy,
        };
    }

    public void Update(
        string companyName,
        string? legalName,
        string? pan,
        string? gstin,
        string? website,
        string? industry,
        DateOnly? incorporationDate,
        string? addressLine1,
        string? addressLine2,
        string? city,
        IndianState? state,
        string? pinCode,
        Guid? filingAddressWorkLocationId,
        Guid updatedBy)
    {
        CompanyName = companyName;
        LegalName = legalName;
        Pan = pan;
        Gstin = gstin;
        Website = website;
        Industry = industry;
        IncorporationDate = incorporationDate;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        PinCode = pinCode;
        FilingAddressWorkLocationId = filingAddressWorkLocationId;
        SetUpdated(updatedBy);
    }

    public void SetLogo(string objectKey, Guid updatedBy)
    {
        LogoObjectKey = objectKey;
        SetUpdated(updatedBy);
    }

    public void ClearLogo(Guid updatedBy)
    {
        LogoObjectKey = null;
        SetUpdated(updatedBy);
    }
}
