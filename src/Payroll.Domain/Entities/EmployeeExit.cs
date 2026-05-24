using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class EmployeeExit : AuditableEntity
{
    private EmployeeExit() { }

    public Guid EmployeeId { get; private set; }
    public DateOnly LastWorkingDay { get; private set; }
    public ExitReason Reason { get; private set; }
    public ExitSettlementMode SettlementMode { get; private set; }
    public DateOnly? SettlementDate { get; private set; }
    public string? PersonalEmail { get; private set; }
    public string? Notes { get; private set; }

    public static EmployeeExit Create(
        Guid employeeId,
        DateOnly lastWorkingDay,
        ExitReason reason,
        ExitSettlementMode settlementMode,
        DateOnly? settlementDate,
        string? personalEmail,
        string? notes,
        Guid createdBy) => new()
        {
            EmployeeId = employeeId,
            LastWorkingDay = lastWorkingDay,
            Reason = reason,
            SettlementMode = settlementMode,
            SettlementDate = settlementDate,
            PersonalEmail = personalEmail,
            Notes = notes,
            CreatedBy = createdBy
        };

    public void Update(
        DateOnly lastWorkingDay,
        ExitReason reason,
        ExitSettlementMode settlementMode,
        DateOnly? settlementDate,
        string? personalEmail,
        string? notes,
        Guid updatedBy)
    {
        LastWorkingDay = lastWorkingDay;
        Reason = reason;
        SettlementMode = settlementMode;
        SettlementDate = settlementDate;
        PersonalEmail = personalEmail;
        Notes = notes;
        SetUpdated(updatedBy);
    }
}
