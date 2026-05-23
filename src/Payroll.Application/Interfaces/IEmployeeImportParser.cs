namespace Payroll.Application.Interfaces;

public record EmployeeImportRow(
    int RowNumber,
    string? EmployeeNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Gender,
    string DateOfJoining,
    string DateOfBirth,
    string WorkEmail,
    string? PersonalEmail,
    string? MobileNumber,
    string? FathersName,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PinCode,
    string? PAN,
    string? Aadhaar,
    string Department,
    string Designation,
    string WorkLocation,
    string EmploymentType,
    string? PaymentMode,
    string? BankAccountHolderName,
    string? BankName,
    string? BankAccountNumber,
    string? IFSCCode,
    string? BankAccountType,
    string? EpfEnabled,
    string? EsiEnabled,
    string? PtEnabled,
    string? LwfEnabled,
    string? UAN,
    string? ESICNumber,
    string? AnnualCTC,
    string? SalaryStructureTemplate);

public record ImportRowError(int RowNumber, string? EmployeeNumber, string Message);

public record ImportValidationResult(
    IReadOnlyList<EmployeeImportRow> NewRows,
    IReadOnlyList<EmployeeImportRow> UpdateRows,
    IReadOnlyList<EmployeeImportRow> SkippedRows,
    IReadOnlyList<ImportRowError> Errors);

public interface IEmployeeImportParser
{
    IReadOnlyList<EmployeeImportRow> Parse(Stream csv);
}
