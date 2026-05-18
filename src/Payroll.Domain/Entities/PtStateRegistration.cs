using Payroll.Domain.Common;

namespace Payroll.Domain.Entities;

public sealed class PtStateRegistration : AuditableEntity
{
    private PtStateRegistration() { }

    public string StateCode { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;

    public static PtStateRegistration Create(string stateCode, string registrationNumber, Guid createdBy) =>
        new()
        {
            StateCode = stateCode,
            RegistrationNumber = registrationNumber,
            CreatedBy = createdBy,
        };

    public void UpdateRegistrationNumber(string registrationNumber, Guid updatedBy)
    {
        RegistrationNumber = registrationNumber;
        SetUpdated(updatedBy);
    }
}
