namespace Payroll.Application.DTOs;

public sealed record BankAdviceRowDto(
    string EmployeeCode,
    string EmployeeName,
    decimal Amount,
    string BankName,
    string BankAccountNo,
    string IfscCode,
    string BeneficiaryName);

public sealed record BankAdviceDto(
    Guid PayrollRunId,
    string PeriodLabel,
    IReadOnlyList<BankAdviceRowDto> Rows);
