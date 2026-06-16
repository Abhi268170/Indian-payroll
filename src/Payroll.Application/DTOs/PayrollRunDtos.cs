namespace Payroll.Application.DTOs;

public sealed record ComponentBreakdownDto(
    Guid Id,
    Guid SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    decimal FullAmount,
    decimal ProratedAmount,
    bool IsOneTimeEarning,
    bool IsDeduction,
    bool IsBenefit);

public sealed record PendingTaskItemDto(Guid EmployeeId, string EmployeeCode, string Reason);

public sealed record PendingTasksDto(
    IReadOnlyList<PendingTaskItemDto> HardBlocks,
    IReadOnlyList<PendingTaskItemDto> SoftWarnings)
{
    public bool HasAnyHardBlocks => HardBlocks.Count > 0;
}

public sealed record EmployeeVariableInputsDto(
    Guid PayrollRunId,
    Guid EmployeeId,
    decimal LopDays,
    int BaseDays,
    decimal ActualPayableDays,
    decimal GrossPay,
    decimal NetPay,
    decimal TdsAmount,
    decimal? TdsOverrideAmount,
    string? TdsOverrideReason,
    decimal EmployeePf,
    decimal EmployerPf,
    decimal EmployeeEsi,
    decimal EmployerEsi,
    decimal PtAmount,
    decimal LwfEmployeeAmount,
    decimal LwfEmployerAmount,
    decimal GratuityAmount,
    decimal EpsAmount,
    decimal MonthlyCTC,
    IReadOnlyList<ComponentBreakdownDto> Components);

public sealed record CurrentPayPeriodDto(
    int Year,
    int Month,
    string PeriodLabel,
    DateOnly? PayDay,
    int ActiveEmployeeCount,
    bool HasOutstandingRun,
    Guid? OutstandingRunId,
    string? OutstandingRunStatus);

public sealed record PayrollRunSummaryDto(
    Guid Id,
    int Year,
    int Month,
    string PeriodLabel,
    string Status,
    string Type,
    DateOnly? PayDay,
    decimal PayrollCost,
    decimal TotalNetPay,
    decimal TotalEmployerPf,
    decimal TotalEmployerEsi,
    decimal TotalTds,
    decimal TotalPt,
    int EmployeeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PaidAt,
    // WI-20: FnF exit metadata, populated only for a single FinalSettlement run
    // (one exit). Null for regular runs and bulk runs (per-employee data lives
    // on the employee rows — see WI-24).
    DateOnly? LastWorkingDay = null,
    string? ExitReason = null,
    string? SettlementMode = null,
    DateOnly? SettlementDate = null);

public sealed record PayrunEmployeeDto(
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Department,
    string Designation,
    string Status,
    decimal LopDays,
    int BaseDays,
    decimal GrossPay,
    decimal NetPay,
    decimal EmployeePf,
    decimal VpfAmount,
    decimal EmployeeEsi,
    decimal PtAmount,
    decimal LwfEmployeeAmount,
    decimal TdsAmount,
    decimal? TdsOverrideAmount,
    // Server-authoritative deductions excluding TDS (statutory + component-level
    // deductions). TDS is shown in its own column, so the summary "Deductions"
    // column must render this rather than re-summing statutory fields client-side.
    decimal DeductionsExTds,
    string? SkipReason,
    // WI-24: exit metadata for FnF run rows (null for regular runs).
    DateOnly? LastWorkingDay = null,
    string? ExitReason = null);

public sealed record PendingRunCardDto(
    Guid Id,
    string Type,
    string Status,
    int Year,
    int Month,
    string PeriodLabel,
    DateOnly? PayDay,
    decimal TotalNetPay,
    int EmployeeCount,
    string? PrimaryEmployeeLabel);

public sealed record PayrollHistoryItemDto(
    Guid Id,
    int Year,
    int Month,
    string PeriodLabel,
    string Type,
    decimal TotalNetPay,
    int EmployeeCount,
    DateOnly? PaymentDate,
    DateTimeOffset? PaidAt);
