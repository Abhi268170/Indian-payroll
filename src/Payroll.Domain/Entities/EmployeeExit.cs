using Payroll.Domain.Common;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Entities;

public sealed class EmployeeExit : AuditableEntity
{
    private EmployeeExit() { }

    public Guid EmployeeId { get; private set; }
    public ExitStatus Status { get; private set; }
    public DateOnly LastWorkingDay { get; private set; }
    public ExitReason Reason { get; private set; }
    public ExitSettlementMode SettlementMode { get; private set; }
    public DateOnly? SettlementDate { get; private set; }
    public string? PersonalEmail { get; private set; }
    public string? Notes { get; private set; }

    // WI-29: current-employer YTD captured at exit initiation. Locks the TDS
    // basis so the FnF recompute is deterministic even if a prior-month run is
    // approved later. Null for exits created before this snapshot existed —
    // the orchestrator falls back to a live YTD query in that case.
    public decimal? YtdGrossSnapshot { get; private set; }
    public decimal? YtdTaxableSnapshot { get; private set; }
    public decimal? YtdTdsSnapshot { get; private set; }

    // Set when the FnF payroll run is created or appended for this exit.
    // For SettlementMode = CustomDate this points at a FinalSettlement run;
    // for RegularSchedule it points at the shared BulkFinalSettlement run for
    // the matched pay date. May be re-linked if the exit moves runs.
    public Guid? FnfPayrollRunId { get; private set; }

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
            Status = ExitStatus.InProgress,
            LastWorkingDay = lastWorkingDay,
            Reason = reason,
            SettlementMode = settlementMode,
            SettlementDate = settlementDate,
            PersonalEmail = personalEmail,
            Notes = notes,
            CreatedBy = createdBy
        };

    public void MarkCompleted(Guid updatedBy)
    {
        Status = ExitStatus.Completed;
        SetUpdated(updatedBy);
    }

    public void MarkReverted(Guid updatedBy)
    {
        Status = ExitStatus.Reverted;
        SetUpdated(updatedBy);
    }

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

    public void SetYtdSnapshot(decimal ytdGross, decimal ytdTaxable, decimal ytdTds, Guid updatedBy)
    {
        YtdGrossSnapshot = ytdGross;
        YtdTaxableSnapshot = ytdTaxable;
        YtdTdsSnapshot = ytdTds;
        SetUpdated(updatedBy);
    }

    public void LinkFnfRun(Guid fnfPayrollRunId, Guid updatedBy)
    {
        FnfPayrollRunId = fnfPayrollRunId;
        SetUpdated(updatedBy);
    }

    public void UnlinkFnfRun(Guid updatedBy)
    {
        FnfPayrollRunId = null;
        SetUpdated(updatedBy);
    }
}
