using ClosedXML.Excel;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Services;

public sealed class SalaryRevisionImportTemplateGenerator : ISalaryRevisionImportTemplateGenerator
{
    private enum ColumnKind { Required, Optional }

    private static readonly (string Header, ColumnKind Kind, string Hint, string? Example)[] Columns =
    [
        ("EmployeeNumber",          ColumnKind.Required, "Existing employee code — must already exist", "EMP001"),
        ("NewAnnualCTC",            ColumnKind.Required, "Revised annual CTC in INR", "1320000"),
        ("EffectiveFromMonth",      ColumnKind.Required, "1-12 (month the revision takes effect)", "3"),
        ("EffectiveFromYear",       ColumnKind.Required, "e.g. 2026", "2026"),
        ("PayoutMonth",             ColumnKind.Required, "1-12 (month arrears are paid)", "5"),
        ("PayoutYear",              ColumnKind.Required, "e.g. 2026", "2026"),
        ("SalaryStructureTemplate", ColumnKind.Required, "Template name — must exist in Settings", "Standard"),
        ("Notes",                   ColumnKind.Optional, "Optional", "Annual hike"),
    ];

    private static readonly XLColor RequiredBg = XLColor.FromHtml("#FECACA"); // red-200
    private static readonly XLColor HeaderText = XLColor.FromHtml("#0f172a");
    private static readonly XLColor HintText = XLColor.FromHtml("#64748b");

    public byte[] Generate()
    {
        using XLWorkbook workbook = new XLWorkbook();
        IXLWorksheet ws = workbook.AddWorksheet("SalaryRevisions");
        IXLWorksheet legend = workbook.AddWorksheet("Instructions");

        BuildDataSheet(ws);
        BuildLegendSheet(legend);

        using MemoryStream ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void BuildDataSheet(IXLWorksheet ws)
    {
        ws.SheetView.FreezeRows(2);

        for (int i = 0; i < Columns.Length; i++)
        {
            int col = i + 1;
            (string header, ColumnKind kind, string hint, string? example) = Columns[i];

            IXLCell headerCell = ws.Cell(1, col);
            headerCell.Value = header;
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Font.FontColor = HeaderText;
            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerCell.Style.Border.BottomBorderColor = XLColor.FromHtml("#e2e8f0");
            if (kind == ColumnKind.Required)
                headerCell.Style.Fill.BackgroundColor = RequiredBg;

            IXLCell hintCell = ws.Cell(2, col);
            hintCell.Value = hint;
            hintCell.Style.Font.Italic = true;
            hintCell.Style.Font.FontSize = 9;
            hintCell.Style.Font.FontColor = HintText;
            hintCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");

            if (example is not null)
                ws.Cell(3, col).Value = example;
        }

        ws.Row(1).Height = 22;
        ws.Row(2).Height = 16;
        ws.Columns().AdjustToContents(minWidth: 14, maxWidth: 30);
    }

    private static void BuildLegendSheet(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "Colour Guide";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;

        ws.Cell(3, 1).Style.Fill.BackgroundColor = RequiredBg;
        ws.Cell(3, 2).Value = "Required — row will fail if left blank";
        ws.Cell(4, 2).Value = "Optional — leave blank to skip";

        ws.Cell(6, 1).Value = "Notes";
        ws.Cell(6, 1).Style.Font.Bold = true;
        ws.Cell(7, 2).Value = "Data starts at row 3. Rows 1-2 are headers and hints — do not delete them.";
        ws.Cell(8, 2).Value = "Arrears are computed automatically for finalised months between Effective-From and Payout.";
        ws.Columns().AdjustToContents();
    }
}
