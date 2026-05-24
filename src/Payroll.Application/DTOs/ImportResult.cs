namespace Payroll.Application.DTOs;

public sealed record ImportResult(int Applied, IReadOnlyList<ImportRowError> Errors);

public sealed record ImportRowError(int Row, string EmployeeCode, string Reason);
