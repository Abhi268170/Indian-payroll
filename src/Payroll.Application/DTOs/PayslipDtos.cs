namespace Payroll.Application.DTOs;

public sealed record PayslipComponentDto(
    string ComponentCode,
    string ComponentName,
    decimal Amount,
    decimal YtdAmount,
    bool IsEarning,
    bool IsBenefit);

public sealed record PayslipData(
    Guid PayrollRunId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Designation,
    string Department,
    string CompanyName,
    string? CompanyAddress,
    int PayPeriodYear,
    int PayPeriodMonth,
    string PeriodLabel,
    DateOnly? PayDay,
    decimal MonthlyCTC,
    decimal GrossPay,
    decimal NetPay,
    string NetPayInWords,
    decimal EmployeePf,
    decimal EmployerPf,
    decimal EmployeeEsi,
    decimal EmployerEsi,
    decimal PtAmount,
    decimal LwfEmployeeAmount,
    decimal TdsAmount,
    decimal GratuityAmount,
    decimal YtdGross,
    decimal YtdNetPay,
    decimal YtdTds,
    decimal YtdPf,
    string MaskedBankAccount,
    string? BankName,
    string? IfscCode,
    IReadOnlyList<PayslipComponentDto> Components,
    // Final-settlement extras: null/false for regular monthly payslips.
    bool IsFinalSettlement = false,
    DateOnly? LastWorkingDay = null,
    string? ExitReason = null,
    string? TenureLabel = null,
    string? ExitNotes = null);

public sealed record PayslipSummaryDto(
    Guid Id,
    Guid PayrollRunId,
    Guid EmployeeId,
    bool IsPublished,
    decimal NetPay,
    string NetPayInWords,
    DateTimeOffset GeneratedAt);
