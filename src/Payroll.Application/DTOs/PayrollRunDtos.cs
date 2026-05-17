namespace Payroll.Application.DTOs;

public sealed record ComponentBreakdownDto(
    Guid Id,
    Guid SalaryComponentId,
    string ComponentCode,
    string ComponentName,
    decimal FullAmount,
    decimal ProratedAmount,
    bool IsOneTimeEarning);

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
    int LopDays,
    int BaseDays,
    int ActualPayableDays,
    decimal GrossPay,
    decimal NetPay,
    decimal TdsAmount,
    decimal? TdsOverrideAmount,
    string? TdsOverrideReason,
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
    DateTimeOffset? PaidAt);
