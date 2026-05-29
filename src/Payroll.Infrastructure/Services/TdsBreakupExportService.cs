using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Calculators;
using Payroll.Engine.Inputs;
using Payroll.Engine.Outputs;

namespace Payroll.Infrastructure.Services;

public sealed class TdsBreakupExportService(
    IPayrollRunRepository runRepo,
    ITdsWorksheetRepository worksheetRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo,
    IPriorEmployerYtdRepository priorYtdRepo)
    : ITdsBreakupExportService
{
    public async Task<ExportFileResult> ExportAsync(Guid runId, string format, CancellationToken ct = default)
    {
        ExportContext ctx = await LoadContextAsync(runId, ct);
        string periodLabel = new DateTime(ctx.Run.PayPeriod.Year, ctx.Run.PayPeriod.Month, 1)
            .ToString("MMM-yyyy", CultureInfo.InvariantCulture);
        bool isXlsx = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(format, "xls", StringComparison.OrdinalIgnoreCase);

        List<TdsRow> rows = BuildRows(ctx);

        if (isXlsx)
        {
            byte[] xlsx = BuildXlsx(rows, periodLabel);
            return new ExportFileResult(
                $"TDSBreakup_{periodLabel}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                xlsx);
        }

        byte[] csv = BuildCsv(rows);
        return new ExportFileResult($"TDSBreakup_{periodLabel}.csv", "text/csv", csv);
    }

    private async Task<ExportContext> LoadContextAsync(Guid runId, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(runId, ct)
            ?? throw new NotFoundException($"Payroll run {runId} not found.");
        if (run.StatutoryConfigSnapshot is null)
            throw new InvalidOperationException($"Payroll run {runId} has no statutory snapshot.");

        StatutoryConfig snapshot = JsonSerializer.Deserialize<StatutoryConfig>(run.StatutoryConfigSnapshot)
            ?? throw new InvalidOperationException("Snapshot deserialize returned null.");

        IReadOnlyList<TdsWorksheet> worksheets = await worksheetRepo.GetByRunIdAsync(runId, ct);
        IReadOnlyList<PayrunEmployee> payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(runId, ct);
        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByIdsAsync(payrunEmps.Select(e => e.EmployeeId), ct);
        int fy = DeriveFiscalYear(run.PayPeriod.Year, run.PayPeriod.Month);
        IReadOnlyList<PriorEmployerYtd> priorYtd = await priorYtdRepo.GetByEmployeesAndFiscalYearAsync(
            employees.Select(e => e.Id).ToList(), fy, ct);

        return new ExportContext(
            run, snapshot, fy,
            worksheets.ToDictionary(w => w.EmployeeId),
            payrunEmps,
            employees.ToDictionary(e => e.Id),
            priorYtd.GroupBy(p => p.EmployeeId).ToDictionary(g => g.Key, g => (IReadOnlyList<PriorEmployerYtd>)g.ToList()));
    }

    private static int DeriveFiscalYear(int year, int month) => month >= 4 ? year : year - 1;

    private static List<TdsRow> BuildRows(ExportContext ctx)
    {
        List<TdsRow> rows = new(ctx.PayrunEmployees.Count);
        foreach (PayrunEmployee pe in ctx.PayrunEmployees)
        {
            Employee? emp = ctx.Employees.GetValueOrDefault(pe.EmployeeId);
            TdsWorksheet? ws = ctx.Worksheets.GetValueOrDefault(pe.EmployeeId);
            IReadOnlyList<PriorEmployerYtd> prior = ctx.PriorYtd.GetValueOrDefault(pe.EmployeeId, []);
            decimal priorTaxable = prior.Sum(p => p.GrossSalary - p.StandardDeductionClaimed);
            decimal priorTds = prior.Sum(p => p.TdsDeducted);

            rows.Add(BuildRow(pe, emp, ws, ctx.Snapshot, ctx.FiscalYear, priorTaxable, priorTds, ctx.Run.PayPeriod));
        }
        return rows;
    }

    private static TdsRow BuildRow(
        PayrunEmployee pe, Employee? emp, TdsWorksheet? ws,
        StatutoryConfig snapshot, int fy, decimal priorTaxable, decimal priorTds,
        Domain.ValueObjects.PayPeriod period)
    {
        bool hasPan = !string.IsNullOrWhiteSpace(emp?.EncryptedPAN);
        TDSWorkingResult? verbose = ws is null
            ? null
            : TDSCalculator.ComputeVerbose(
                annualProjectedGross: ws.AnnualProjectedIncome,
                priorEmployerYTDTaxableIncome: 0m,
                priorEmployerYTDTDSDeducted: priorTds,
                currentEmployerYTDTDSDeducted: ws.YtdTdsDeducted,
                hasPan: hasPan,
                config: snapshot,
                monthsRemainingInFY: ws.RemainingMonthsInFy);

        return new TdsRow(pe, emp, ws, verbose, fy, priorTaxable, priorTds, period);
    }

    private static List<string> Headers =>
    [
        "Employee Code", "Employee Name", "PAN Furnished", "Regime",
        "Pay Month", "FY", "Months Remaining in FY", "Status",
        "Monthly Taxable Gross", "Annual Projected Gross",
        "Prior Employer YTD Taxable Income", "Total Projected Income",
        "Standard Deduction", "Taxable Income",
        "Tax @ 0–4L (0%)", "Tax @ 4L–8L (5%)", "Tax @ 8L–12L (10%)",
        "Tax @ 12L–16L (15%)", "Tax @ 16L–20L (20%)", "Tax @ 20L–24L (25%)",
        "Tax @ Above 24L (30%)", "Tax Before Rebate",
        "87A Rebate Applied", "87A Rebate Amount", "Tax After Rebate",
        "Surcharge Slab Rate", "Raw Surcharge", "Marginal Relief Applied", "Surcharge After Relief",
        "Subtotal (Tax + Surcharge)", "Cess Rate", "Cess Amount",
        "Total Annual Tax Liability", "Prior Employer TDS Deducted",
        "Current Employer YTD TDS Deducted", "Remaining Tax for FY",
        "Monthly TDS (Engine)", "Monthly TDS Override", "Override Reason",
        "Effective TDS This Run",
        "206AA 20% Flat Annual", "206AA Monthly TDS",
    ];

    private static byte[] BuildCsv(List<TdsRow> rows)
    {
        StringBuilder sb = new();
        sb.AppendLine(string.Join(",", Headers.Select(CsvEscape)));
        foreach (TdsRow r in rows)
            sb.AppendLine(string.Join(",", BuildRowValues(r).Select(CsvEscape)));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] BuildXlsx(List<TdsRow> rows, string periodLabel)
    {
        using XLWorkbook workbook = new();
        IXLWorksheet ws = workbook.AddWorksheet($"TDS {periodLabel}");

        for (int i = 0; i < Headers.Count; i++)
        {
            IXLCell cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (TdsRow r in rows)
        {
            List<string> values = BuildRowValues(r);
            for (int i = 0; i < values.Count; i++)
            {
                IXLCell cell = ws.Cell(row, i + 1);
                if (decimal.TryParse(values[i], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal num))
                {
                    cell.Value = num;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else cell.Value = values[i];
            }
            row++;
        }

        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(2);
        ws.Range(1, 1, row - 1, Headers.Count).SetAutoFilter();
        ws.Columns().AdjustToContents();

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<string> BuildRowValues(TdsRow r)
    {
        string panFurnished = string.IsNullOrWhiteSpace(r.Employee?.EncryptedPAN) ? "No" : "Yes";
        string status = r.PayrunEmployee.Status.ToString();
        string payMonth = new DateTime(r.Period.Year, r.Period.Month, 1).ToString("MMM-yyyy", CultureInfo.InvariantCulture);
        string fyLabel = $"{r.FiscalYear}-{(r.FiscalYear + 1) % 100:D2}";

        if (r.Worksheet is null)
        {
            return WithBlanks(r, panFurnished, status, payMonth, fyLabel);
        }

        TDSWorkingResult v = r.Verbose!;
        SlabTax[] slabs = v.SlabBreakdown.ToArray();

        decimal effectiveTds = r.PayrunEmployee.TdsOverrideAmount ?? r.Worksheet.TdsThisMonth;
        string overrideAmount = r.PayrunEmployee.TdsOverrideAmount.HasValue ? FormatMoney(r.PayrunEmployee.TdsOverrideAmount.Value) : string.Empty;

        List<string> row =
        [
            r.Employee?.EmployeeCode ?? string.Empty,
            r.Employee?.FullName ?? string.Empty,
            panFurnished, "New (Sec 115BAC)", payMonth, fyLabel,
            FormatInt(r.Worksheet.RemainingMonthsInFy), status,
            FormatMoney(r.PayrunEmployee.TaxableGrossPay),
            FormatMoney(r.Worksheet.AnnualProjectedIncome - r.PriorTaxable),
            FormatMoney(r.PriorTaxable),
            FormatMoney(r.Worksheet.AnnualProjectedIncome),
            FormatMoney(r.Worksheet.StandardDeduction),
            FormatMoney(r.Worksheet.TaxableIncome),
        ];

        for (int i = 0; i < 7; i++)
            row.Add(i < slabs.Length ? FormatMoney(slabs[i].Tax) : "0.00");

        row.Add(FormatMoney(r.Worksheet.TaxBeforeRebate));
        row.Add(v.Rebate87AApplied ? "Yes" : "No");
        row.Add(FormatMoney(v.Rebate87AAmount));
        row.Add(FormatMoney(v.TaxAfterRebate));
        row.Add(v.SurchargeRate.HasValue ? FormatPercent(v.SurchargeRate.Value) : string.Empty);
        row.Add(FormatMoney(v.RawSurcharge));
        row.Add(v.MarginalReliefApplied ? "Yes" : "No");
        row.Add(FormatMoney(v.SurchargeAfterRelief));
        row.Add(FormatMoney(v.TaxAfterRebate + v.SurchargeAfterRelief));
        row.Add(FormatPercent(r.Worksheet.AnnualTaxLiability == 0m ? 0m : v.CessRate));
        row.Add(FormatMoney(r.Worksheet.Cess));
        row.Add(FormatMoney(r.Worksheet.AnnualTaxLiability));
        row.Add(FormatMoney(r.PriorTds));
        row.Add(FormatMoney(r.Worksheet.YtdTdsDeducted));
        row.Add(FormatMoney(v.RemainingTaxForFY));
        row.Add(FormatMoney(r.Worksheet.TdsThisMonth));
        row.Add(overrideAmount);
        row.Add(r.PayrunEmployee.TdsOverrideReason ?? string.Empty);
        row.Add(FormatMoney(effectiveTds));
        row.Add(r.Worksheet.HasPanOverride && v.Pan206AAAnnual.HasValue ? FormatMoney(v.Pan206AAAnnual.Value) : string.Empty);
        row.Add(r.Worksheet.HasPanOverride && v.Pan206AAMonthly.HasValue ? FormatMoney(v.Pan206AAMonthly.Value) : string.Empty);
        return row;
    }

    private static List<string> WithBlanks(TdsRow r, string panFurnished, string status, string payMonth, string fyLabel)
    {
        List<string> row =
        [
            r.Employee?.EmployeeCode ?? string.Empty,
            r.Employee?.FullName ?? string.Empty,
            panFurnished, "New (Sec 115BAC)", payMonth, fyLabel,
            string.Empty, status,
        ];
        for (int i = 0; i < Headers.Count - row.Count; i++) row.Add(string.Empty);
        return row;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string FormatMoney(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    private static string FormatInt(int v) => v.ToString(CultureInfo.InvariantCulture);
    private static string FormatPercent(decimal v) => (v * 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    private sealed record TdsRow(
        PayrunEmployee PayrunEmployee,
        Employee? Employee,
        TdsWorksheet? Worksheet,
        TDSWorkingResult? Verbose,
        int FiscalYear,
        decimal PriorTaxable,
        decimal PriorTds,
        Domain.ValueObjects.PayPeriod Period);

    private sealed record ExportContext(
        PayrollRun Run,
        StatutoryConfig Snapshot,
        int FiscalYear,
        Dictionary<Guid, TdsWorksheet> Worksheets,
        IReadOnlyList<PayrunEmployee> PayrunEmployees,
        Dictionary<Guid, Employee> Employees,
        Dictionary<Guid, IReadOnlyList<PriorEmployerYtd>> PriorYtd);
}
