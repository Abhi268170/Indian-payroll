using ClosedXML.Excel;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;

namespace Payroll.Infrastructure.Services;

public sealed class SalaryRevisionImportParser : ISalaryRevisionImportParser
{
    private static readonly string[] ExpectedHeaders =
    [
        "EmployeeNumber", "NewAnnualCTC", "EffectiveFromMonth", "EffectiveFromYear",
        "PayoutMonth", "PayoutYear", "SalaryStructureTemplate", "Notes"
    ];

    public IReadOnlyList<SalaryRevisionImportRow> Parse(Stream xlsx)
    {
        using XLWorkbook workbook = new(xlsx);
        IXLWorksheet ws = workbook.Worksheets.First();

        // Row 1 = headers, row 2 = hints — data starts at row 3 (same layout as employee import).
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

        List<SalaryRevisionImportRow> rows = [];
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 2;
        for (int r = 3; r <= lastRow; r++)
        {
            IXLRow row = ws.Row(r);
            if (row.IsEmpty()) continue;

            rows.Add(new SalaryRevisionImportRow(
                RowNumber: r,
                EmployeeNumber: Get(row, colIndex, "EmployeeNumber"),
                NewAnnualCTC: Get(row, colIndex, "NewAnnualCTC"),
                EffectiveFromMonth: Get(row, colIndex, "EffectiveFromMonth"),
                EffectiveFromYear: Get(row, colIndex, "EffectiveFromYear"),
                PayoutMonth: Get(row, colIndex, "PayoutMonth"),
                PayoutYear: Get(row, colIndex, "PayoutYear"),
                SalaryStructureTemplate: Get(row, colIndex, "SalaryStructureTemplate"),
                Notes: Get(row, colIndex, "Notes")));
        }

        return rows;
    }

    private static string? Get(IXLRow row, Dictionary<string, int> colIndex, string header)
    {
        if (!colIndex.TryGetValue(header, out int col)) return null;
        string value = row.Cell(col).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
