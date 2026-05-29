using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Services;

public sealed class PayrollDetailsExportService(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    IEmployeeRepository employeeRepo,
    ISalaryComponentRepository componentRepo,
    IDepartmentRepository departmentRepo,
    IDesignationRepository designationRepo,
    IWorkLocationRepository workLocationRepo)
    : IPayrollDetailsExportService
{
    public async Task<ExportFileResult> ExportAsync(Guid runId, string format, CancellationToken ct = default)
    {
        ExportContext ctx = await LoadContextAsync(runId, ct);
        string periodLabel = new DateTime(ctx.Run.PayPeriod.Year, ctx.Run.PayPeriod.Month, 1).ToString("MMM-yyyy", CultureInfo.InvariantCulture);
        bool isXlsx = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "xls", StringComparison.OrdinalIgnoreCase);

        if (isXlsx)
        {
            byte[] xls = BuildXlsx(ctx, periodLabel);
            return new ExportFileResult(
                $"PayrollDetails_{periodLabel}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                xls);
        }

        byte[] csv = BuildCsv(ctx);
        return new ExportFileResult($"PayrollDetails_{periodLabel}.csv", "text/csv", csv);
    }

    private async Task<ExportContext> LoadContextAsync(Guid runId, CancellationToken ct)
    {
        PayrollRun run = await runRepo.GetByIdAsync(runId, ct)
            ?? throw new NotFoundException($"Payroll run {runId} not found.");

        IReadOnlyList<PayrunEmployee> payrunEmps = await payrunEmployeeRepo.GetByRunIdAsync(runId, ct);
        IReadOnlyList<PayrunComponentBreakdown> breakdowns = await breakdownRepo.GetByRunIdAsync(runId, ct);
        IReadOnlyList<Employee> employees = await employeeRepo.GetManyByIdsAsync(payrunEmps.Select(e => e.EmployeeId), ct);
        List<SalaryComponent> components = await componentRepo.ListByTenantAsync(run.TenantId, ct: ct);
        IReadOnlyList<Department> departments = await departmentRepo.ListAsync(ct);
        IReadOnlyList<Designation> designations = await designationRepo.ListAsync(ct);
        IReadOnlyList<WorkLocation> workLocations = await workLocationRepo.ListAsync(ct);

        return new ExportContext(
            run, payrunEmps, breakdowns,
            employees.ToDictionary(e => e.Id),
            components.ToDictionary(c => c.Id),
            departments.ToDictionary(d => d.Id),
            designations.ToDictionary(d => d.Id),
            workLocations.ToDictionary(w => w.Id));
    }

    private static List<ComponentColumn> BuildComponentColumns(ExportContext ctx, ComponentCategory category, bool benefitsOnly = false)
    {
        HashSet<string> codes = ctx.Breakdowns
            .Where(b => MatchesCategory(b, ctx, category, benefitsOnly))
            .Select(b => b.ComponentCode)
            .ToHashSet();
        return codes
            .Select(code => new ComponentColumn(code, ResolveDisplayName(code, ctx)))
            .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool MatchesCategory(PayrunComponentBreakdown b, ExportContext ctx, ComponentCategory category, bool benefitsOnly)
    {
        if (benefitsOnly) return b.IsBenefit;
        if (b.IsBenefit) return false;
        if (b.SalaryComponentId is null) return false;
        return ctx.Components.TryGetValue(b.SalaryComponentId.Value, out SalaryComponent? sc) && sc.Category == category;
    }

    private static string ResolveDisplayName(string code, ExportContext ctx)
    {
        PayrunComponentBreakdown? sample = ctx.Breakdowns.FirstOrDefault(b => b.ComponentCode == code);
        return sample?.ComponentName ?? code;
    }

    private static decimal? GetComponentAmount(IEnumerable<PayrunComponentBreakdown> empBreakdowns, string code)
    {
        PayrunComponentBreakdown? hit = empBreakdowns.FirstOrDefault(b => b.ComponentCode == code);
        return hit?.ProratedAmount;
    }

    private static byte[] BuildCsv(ExportContext ctx)
    {
        List<ComponentColumn> earningCols = BuildComponentColumns(ctx, ComponentCategory.Earning);
        List<ComponentColumn> benefitCols = BuildComponentColumns(ctx, ComponentCategory.Earning, benefitsOnly: true);
        List<ComponentColumn> reimbursementCols = BuildComponentColumns(ctx, ComponentCategory.Reimbursement);
        List<ComponentColumn> deductionCols = BuildComponentColumns(ctx, ComponentCategory.Deduction);

        StringBuilder sb = new();
        sb.AppendLine(string.Join(",", BuildHeaderRow(earningCols, benefitCols, reimbursementCols, deductionCols).Select(CsvEscape)));

        ILookup<Guid, PayrunComponentBreakdown> byEmployee = ctx.Breakdowns.ToLookup(b => b.EmployeeId);
        foreach (PayrunEmployee pe in ctx.PayrunEmployees)
        {
            IEnumerable<PayrunComponentBreakdown> empBd = byEmployee[pe.EmployeeId];
            sb.AppendLine(string.Join(",", BuildDataRow(pe, empBd, ctx, earningCols, benefitCols, reimbursementCols, deductionCols).Select(CsvEscape)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static List<string> BuildHeaderRow(
        List<ComponentColumn> earnings, List<ComponentColumn> benefits,
        List<ComponentColumn> reimbursements, List<ComponentColumn> deductions)
    {
        List<string> headers =
        [
            "Employee Code", "Employee Name", "Designation", "Department", "Work Location",
            "Date of Joining", "Date of Leaving", "Status", "Skip Reason",
            "Base Days", "LOP Days", "Payable Days",
        ];
        headers.AddRange(earnings.Select(c => c.DisplayName));
        headers.Add("Gross Pay");
        headers.Add("Taxable Gross");
        headers.AddRange(benefits.Select(c => c.DisplayName));
        headers.Add("Total Benefits");
        headers.AddRange(reimbursements.Select(c => c.DisplayName));
        headers.Add("Total Reimbursements");
        headers.AddRange(["Employee PF", "Employee ESI", "Professional Tax", "LWF (Employee)", "TDS", "TDS Override", "TDS Override Reason"]);
        headers.AddRange(deductions.Select(c => c.DisplayName));
        headers.AddRange(["Total Deductions", "Net Pay"]);
        headers.AddRange(["Employer PF", "EPS", "Employer ESI", "LWF (Employer)", "Gratuity Accrual", "Total Employer Cost"]);
        headers.AddRange(["Monthly CTC", "Annual CTC", "CTC Reconciliation Δ"]);
        return headers;
    }

    private static List<string> BuildDataRow(
        PayrunEmployee pe, IEnumerable<PayrunComponentBreakdown> empBd, ExportContext ctx,
        List<ComponentColumn> earnings, List<ComponentColumn> benefits,
        List<ComponentColumn> reimbursements, List<ComponentColumn> deductions)
    {
        Employee? emp = ctx.Employees.GetValueOrDefault(pe.EmployeeId);
        List<string> row =
        [
            emp?.EmployeeCode ?? string.Empty,
            emp?.FullName ?? string.Empty,
            emp is null ? string.Empty : ctx.Designations.GetValueOrDefault(emp.DesignationId)?.Name ?? string.Empty,
            emp is null ? string.Empty : ctx.Departments.GetValueOrDefault(emp.DepartmentId)?.Name ?? string.Empty,
            emp is null ? string.Empty : ctx.WorkLocations.GetValueOrDefault(emp.WorkLocationId)?.Name ?? string.Empty,
            emp?.DateOfJoining.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
            emp?.DateOfLeaving?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty,
            pe.Status.ToString(),
            pe.SkipReason ?? string.Empty,
            FormatInt(pe.BaseDays), FormatInt(pe.LopDays), FormatInt(pe.ActualPayableDays),
        ];
        decimal benefitsTotal = empBd.Where(b => b.IsBenefit).Sum(b => b.ProratedAmount);
        decimal reimbursementsTotal = pe.ReimbursementsAmount;
        decimal otherDeductionsTotal = deductions.Sum(c => GetComponentAmount(empBd, c.Code) ?? 0m);
        decimal effectiveTds = pe.TdsOverrideAmount ?? pe.TdsAmount;
        decimal totalDeductions = pe.EmployeePf + pe.EmployeeEsi + pe.PtAmount + pe.LwfEmployeeAmount + effectiveTds + otherDeductionsTotal;
        decimal employerCost = pe.EmployerPf + pe.EpsAmount + pe.EmployerEsi + pe.LwfEmployerAmount + pe.GratuityAmount + benefitsTotal;
        decimal annualCtc = pe.MonthlyCTC * 12m;
        decimal ctcDelta = pe.MonthlyCTC - (pe.GrossPay + employerCost);

        foreach (ComponentColumn c in earnings) row.Add(FormatMoneyOrEmpty(GetComponentAmount(empBd, c.Code)));
        row.Add(FormatMoney(pe.GrossPay));
        row.Add(FormatMoney(pe.TaxableGrossPay));
        foreach (ComponentColumn c in benefits) row.Add(FormatMoneyOrEmpty(GetComponentAmount(empBd, c.Code)));
        row.Add(FormatMoney(benefitsTotal));
        foreach (ComponentColumn c in reimbursements) row.Add(FormatMoneyOrEmpty(GetComponentAmount(empBd, c.Code)));
        row.Add(FormatMoney(reimbursementsTotal));
        row.AddRange([FormatMoney(pe.EmployeePf), FormatMoney(pe.EmployeeEsi), FormatMoney(pe.PtAmount), FormatMoney(pe.LwfEmployeeAmount), FormatMoney(pe.TdsAmount), pe.TdsOverrideAmount.HasValue ? FormatMoney(pe.TdsOverrideAmount.Value) : string.Empty, pe.TdsOverrideReason ?? string.Empty]);
        foreach (ComponentColumn c in deductions) row.Add(FormatMoneyOrEmpty(GetComponentAmount(empBd, c.Code)));
        row.AddRange([FormatMoney(totalDeductions), FormatMoney(pe.NetPay)]);
        row.AddRange([FormatMoney(pe.EmployerPf), FormatMoney(pe.EpsAmount), FormatMoney(pe.EmployerEsi), FormatMoney(pe.LwfEmployerAmount), FormatMoney(pe.GratuityAmount), FormatMoney(employerCost)]);
        row.AddRange([FormatMoney(pe.MonthlyCTC), FormatMoney(annualCtc), FormatMoney(ctcDelta)]);
        return row;
    }

    private static byte[] BuildXlsx(ExportContext ctx, string periodLabel)
    {
        List<ComponentColumn> earningCols = BuildComponentColumns(ctx, ComponentCategory.Earning);
        List<ComponentColumn> benefitCols = BuildComponentColumns(ctx, ComponentCategory.Earning, benefitsOnly: true);
        List<ComponentColumn> reimbursementCols = BuildComponentColumns(ctx, ComponentCategory.Reimbursement);
        List<ComponentColumn> deductionCols = BuildComponentColumns(ctx, ComponentCategory.Deduction);

        List<string> headers = BuildHeaderRow(earningCols, benefitCols, reimbursementCols, deductionCols);
        using XLWorkbook workbook = new();
        IXLWorksheet ws = workbook.AddWorksheet($"Payroll {periodLabel}");

        WriteHeaderRow(ws, headers);
        int row = 2;
        ILookup<Guid, PayrunComponentBreakdown> byEmployee = ctx.Breakdowns.ToLookup(b => b.EmployeeId);
        foreach (PayrunEmployee pe in ctx.PayrunEmployees)
        {
            List<string> values = BuildDataRow(pe, byEmployee[pe.EmployeeId], ctx, earningCols, benefitCols, reimbursementCols, deductionCols);
            WriteDataRow(ws, row, values);
            row++;
        }
        ApplyXlsxFormatting(ws, headers.Count, row - 1);

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteHeaderRow(IXLWorksheet ws, List<string> headers)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            IXLCell cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void WriteDataRow(IXLWorksheet ws, int row, List<string> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            string raw = values[i];
            IXLCell cell = ws.Cell(row, i + 1);
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal num))
            {
                cell.Value = num;
                cell.Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                cell.Value = raw;
            }
        }
    }

    private static void ApplyXlsxFormatting(IXLWorksheet ws, int colCount, int lastDataRow)
    {
        ws.SheetView.FreezeRows(1);
        ws.SheetView.FreezeColumns(2);
        ws.Range(1, 1, lastDataRow, colCount).SetAutoFilter();
        ws.Columns().AdjustToContents();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string FormatMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatMoneyOrEmpty(decimal? value) =>
        value.HasValue ? FormatMoney(value.Value) : string.Empty;

    private static string FormatInt(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private sealed record ComponentColumn(string Code, string DisplayName);

    private sealed record ExportContext(
        PayrollRun Run,
        IReadOnlyList<PayrunEmployee> PayrunEmployees,
        IReadOnlyList<PayrunComponentBreakdown> Breakdowns,
        Dictionary<Guid, Employee> Employees,
        Dictionary<Guid, SalaryComponent> Components,
        Dictionary<Guid, Department> Departments,
        Dictionary<Guid, Designation> Designations,
        Dictionary<Guid, WorkLocation> WorkLocations);
}
