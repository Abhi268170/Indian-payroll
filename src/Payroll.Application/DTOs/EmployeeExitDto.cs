namespace Payroll.Application.DTOs;

public sealed record EmployeeExitDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly LastWorkingDay,
    string Reason,
    string SettlementMode,
    DateOnly? SettlementDate,
    string? PersonalEmail,
    string? Notes,
    Guid FnfPayrollRunId,
    string FnfPayrollRunType,
    DateOnly? FnfPayDate);
