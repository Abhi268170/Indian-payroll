using ClosedXML.Excel;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;

namespace Payroll.Infrastructure.Services;

public sealed class EmployeeImportParser : IEmployeeImportParser
{
    private static readonly string[] ExpectedHeaders =
    [
        "EmployeeNumber", "FirstName", "MiddleName", "LastName", "Gender",
        "DateOfJoining", "DateOfBirth", "WorkEmail", "PersonalEmail", "MobileNumber",
        "FathersName", "AddressLine1", "AddressLine2", "City", "State", "PinCode",
        "PAN", "Aadhaar", "Department", "Designation", "WorkLocation", "EmploymentType",
        "PaymentMode", "BankAccountHolderName", "BankName", "BankAccountNumber",
        "IFSCCode", "BankAccountType", "EpfEnabled", "EsiEnabled", "PtEnabled",
        "LwfEnabled", "UAN", "ESICNumber", "AnnualCTC", "SalaryStructureTemplate"
    ];

    public IReadOnlyList<EmployeeImportRow> Parse(Stream xlsx)
    {
        using XLWorkbook workbook = new(xlsx);
        IXLWorksheet ws = workbook.Worksheets.First();

        // Row 1 = headers, row 2 = hints — data starts at row 3
        IXLRow headerRow = ws.Row(1);

        Dictionary<string, int> colIndex = [];
        foreach (IXLCell cell in headerRow.CellsUsed())
        {
            string header = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                colIndex[header] = cell.Address.ColumnNumber;
        }

        List<string> missing = ExpectedHeaders.Where(h => !colIndex.ContainsKey(h)).ToList();
        if (missing.Count > 0)
            throw new ImportFormatException(
                $"File is missing columns: {string.Join(", ", missing)}. Download the template and try again.");

        List<EmployeeImportRow> rows = [];
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 2;

        // Start from row 3 — row 1 is headers, row 2 is hints
        for (int r = 3; r <= lastRow; r++)
        {
            IXLRow row = ws.Row(r);

            // Skip entirely blank rows
            if (row.IsEmpty()) continue;

            rows.Add(new EmployeeImportRow(
                RowNumber: r,
                EmployeeNumber: Get(row, colIndex, "EmployeeNumber"),
                FirstName: GetRequired(row, colIndex, "FirstName"),
                MiddleName: Get(row, colIndex, "MiddleName"),
                LastName: GetRequired(row, colIndex, "LastName"),
                Gender: GetRequired(row, colIndex, "Gender"),
                DateOfJoining: GetRequired(row, colIndex, "DateOfJoining"),
                DateOfBirth: GetRequired(row, colIndex, "DateOfBirth"),
                WorkEmail: GetRequired(row, colIndex, "WorkEmail").ToLowerInvariant(),
                PersonalEmail: Get(row, colIndex, "PersonalEmail"),
                MobileNumber: Get(row, colIndex, "MobileNumber"),
                FathersName: Get(row, colIndex, "FathersName"),
                AddressLine1: Get(row, colIndex, "AddressLine1"),
                AddressLine2: Get(row, colIndex, "AddressLine2"),
                City: Get(row, colIndex, "City"),
                State: Get(row, colIndex, "State"),
                PinCode: Get(row, colIndex, "PinCode"),
                PAN: Get(row, colIndex, "PAN")?.ToUpperInvariant(),
                Aadhaar: Get(row, colIndex, "Aadhaar"),
                Department: GetRequired(row, colIndex, "Department"),
                Designation: GetRequired(row, colIndex, "Designation"),
                WorkLocation: GetRequired(row, colIndex, "WorkLocation"),
                EmploymentType: GetRequired(row, colIndex, "EmploymentType"),
                PaymentMode: Get(row, colIndex, "PaymentMode"),
                BankAccountHolderName: Get(row, colIndex, "BankAccountHolderName"),
                BankName: Get(row, colIndex, "BankName"),
                BankAccountNumber: Get(row, colIndex, "BankAccountNumber"),
                IFSCCode: Get(row, colIndex, "IFSCCode")?.ToUpperInvariant(),
                BankAccountType: Get(row, colIndex, "BankAccountType"),
                EpfEnabled: Get(row, colIndex, "EpfEnabled"),
                EsiEnabled: Get(row, colIndex, "EsiEnabled"),
                PtEnabled: Get(row, colIndex, "PtEnabled"),
                LwfEnabled: Get(row, colIndex, "LwfEnabled"),
                UAN: Get(row, colIndex, "UAN"),
                ESICNumber: Get(row, colIndex, "ESICNumber"),
                AnnualCTC: Get(row, colIndex, "AnnualCTC"),
                SalaryStructureTemplate: Get(row, colIndex, "SalaryStructureTemplate")
            ));
        }

        return rows;
    }

    private static string? Get(IXLRow row, Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out int colNum)) return null;
        IXLCell cell = row.Cell(colNum);
        string val = cell.GetString().Trim();
        return string.IsNullOrEmpty(val) ? null : val;
    }

    private static string GetRequired(IXLRow row, Dictionary<string, int> idx, string col) =>
        Get(row, idx, col) ?? string.Empty;
}
