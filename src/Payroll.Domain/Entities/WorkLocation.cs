using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class WorkLocation : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public IndianState State { get; private set; }
    public string? City { get; private set; }
    public string? PinCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    private WorkLocation() { }  // EF constructor

    public static WorkLocation Create(
        string name,
        IndianState state,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? pinCode,
        Guid createdBy)
    {
        WorkLocation wl = new()
        {
            Name = name,
            State = state,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            PinCode = pinCode,
            CreatedBy = createdBy,
        };
        return wl;
    }

    public void Update(
        string name,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? pinCode,
        Guid updatedBy)
    {
        // State is NOT updated — immutable after creation
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        PinCode = pinCode;
        SetUpdated(updatedBy);
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        SetUpdated(updatedBy);
    }

    public void Activate(Guid updatedBy)
    {
        IsActive = true;
        SetUpdated(updatedBy);
    }
}
