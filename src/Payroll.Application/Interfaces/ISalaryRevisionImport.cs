namespace Payroll.Application.Interfaces;

public record SalaryRevisionImportRow(
    int RowNumber,
    string? EmployeeNumber,
    string? NewAnnualCTC,
    string? EffectiveFromMonth,
    string? EffectiveFromYear,
    string? PayoutMonth,
    string? PayoutYear,
    string? SalaryStructureTemplate,
    string? Notes);

public record SalaryRevisionImportRowError(int RowNumber, string? EmployeeNumber, string Message);

public record SalaryRevisionImportValidationResult(
    IReadOnlyList<SalaryRevisionImportRow> ValidRows,
    IReadOnlyList<SalaryRevisionImportRow> SkippedRows,
    IReadOnlyList<SalaryRevisionImportRowError> Errors);

public interface ISalaryRevisionImportParser
{
    IReadOnlyList<SalaryRevisionImportRow> Parse(Stream xlsx);
}

public interface ISalaryRevisionImportTemplateGenerator
{
    byte[] Generate();
}
