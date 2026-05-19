using ClosedXML.Excel;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Services;

public sealed class BankAdviceGenerator : IBankAdviceGenerator
{
    private static readonly string[] Headers =
    [
        "Employee No",
        "Employee Name",
        "Amount",
        "Bank Name",
        "Bank Account No",
        "IFSC Code",
        "Beneficiary Name",
    ];

    public byte[] Generate(BankAdviceDto data)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Bank Statement");

        for (int col = 1; col <= Headers.Length; col++)
        {
            var cell = ws.Cell(1, col);
            cell.Value = Headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (BankAdviceRowDto item in data.Rows)
        {
            ws.Cell(row, 1).Value = item.EmployeeCode;
            ws.Cell(row, 2).Value = item.EmployeeName;
            ws.Cell(row, 3).Value = (double)item.Amount;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = item.BankName;
            ws.Cell(row, 5).Value = item.BankAccountNo;
            ws.Cell(row, 6).Value = item.IfscCode;
            ws.Cell(row, 7).Value = item.BeneficiaryName;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
