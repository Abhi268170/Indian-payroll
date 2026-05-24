using System.Text;
using ClosedXML.Excel;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Services;

public sealed class PayrollExportService(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo)
    : IPayrollExportService
{
    private static readonly string[] Headers =
    [
        "Employee Code", "Employee Name", "Paid Days", "LOP Days",
        "Gross Pay", "Employee PF", "Employee ESI", "TDS", "PT", "LWF (Employee)", "Net Pay", "Status"
    ];

    public async Task<ExportFileResult> ExportEmployeePayRunDetailsAsync(Guid runId, string format, CancellationToken ct = default)
    {
        var run = await runRepo.GetByIdAsync(runId, ct)
            ?? throw new NotFoundException($"Payroll run {runId} not found.");

        IReadOnlyList<PayrunEmployee> payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(runId, ct);

        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByIdsAsync(payrunEmps.Select(e => e.EmployeeId), ct);
        Dictionary<Guid, Employee> empById = employees.ToDictionary(e => e.Id);

        string periodLabel = new DateTime(run.PayPeriod.Year, run.PayPeriod.Month, 1).ToString("MMM-yyyy");

        if (string.Equals(format, "xls", StringComparison.OrdinalIgnoreCase))
        {
            byte[] xls = BuildXls(payrunEmps, empById, periodLabel);
            return new ExportFileResult($"Payroll_{periodLabel}.xls", "application/vnd.ms-excel", xls);
        }

        byte[] csv = BuildCsv(payrunEmps, empById);
        return new ExportFileResult($"Payroll_{periodLabel}.csv", "text/csv", csv);
    }

    private static byte[] BuildCsv(IReadOnlyList<PayrunEmployee> payrunEmps, Dictionary<Guid, Employee> empById)
    {
        StringBuilder sb = new();
        sb.AppendLine(string.Join(",", Headers));

        foreach (PayrunEmployee pe in payrunEmps)
        {
            empById.TryGetValue(pe.EmployeeId, out Employee? emp);
            string code = emp?.EmployeeCode ?? pe.EmployeeId.ToString();
            string name = CsvEscape(emp?.FullName ?? string.Empty);
            string status = pe.Status == PayrunEmployeeStatus.Skipped ? "Skipped" : "Active";

            sb.AppendLine($"{code},{name},{pe.ActualPayableDays},{pe.LopDays}," +
                          $"{pe.GrossPay},{pe.EmployeePf},{pe.EmployeeEsi}," +
                          $"{pe.TdsAmount},{pe.PtAmount},{pe.LwfEmployeeAmount},{pe.NetPay},{status}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildXls(IReadOnlyList<PayrunEmployee> payrunEmps, Dictionary<Guid, Employee> empById, string periodLabel)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet($"Payroll {periodLabel}");

        for (int col = 1; col <= Headers.Length; col++)
        {
            var cell = ws.Cell(1, col);
            cell.Value = Headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (PayrunEmployee pe in payrunEmps)
        {
            empById.TryGetValue(pe.EmployeeId, out Employee? emp);
            ws.Cell(row, 1).Value = emp?.EmployeeCode ?? pe.EmployeeId.ToString();
            ws.Cell(row, 2).Value = emp?.FullName ?? string.Empty;
            ws.Cell(row, 3).Value = pe.ActualPayableDays;
            ws.Cell(row, 4).Value = pe.LopDays;
            ws.Cell(row, 5).Value = pe.GrossPay;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Value = pe.EmployeePf;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = pe.EmployeeEsi;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Value = pe.TdsAmount;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Value = pe.PtAmount;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 10).Value = pe.LwfEmployeeAmount;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 11).Value = pe.NetPay;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 12).Value = pe.Status == PayrunEmployeeStatus.Skipped ? "Skipped" : "Active";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
