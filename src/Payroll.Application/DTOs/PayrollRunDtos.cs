namespace Payroll.Application.DTOs;

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
