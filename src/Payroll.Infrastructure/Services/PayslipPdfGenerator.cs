using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Payroll.Infrastructure.Services;

public sealed class PayslipPdfGenerator : IPayslipPdfGenerator
{
    private const string FontFamily = "Arial";
    private static readonly Color HeaderBg = Color.FromHex("#1e293b");
    private static readonly Color SectionHeaderBg = Color.FromHex("#f1f5f9");
    private static readonly Color BorderColor = Color.FromHex("#e2e8f0");

    static PayslipPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(PayslipData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, Unit.Point);
                page.DefaultTextStyle(t => t.FontFamily(FontFamily).FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, data));
                    col.Item().Height(8);
                    col.Item().Element(c => ComposeEmployeeDetails(c, data));
                    col.Item().Height(8);
                    col.Item().Element(c => ComposeEarningsDeductions(c, data));
                    if (data.MonthlyCTC > data.GrossPay)
                    {
                        col.Item().Height(8);
                        col.Item().Element(c => ComposeCTCBreakdown(c, data));
                    }
                    col.Item().Height(8);
                    col.Item().Element(c => ComposeNetPaySummary(c, data));
                    col.Item().Height(8);
                    col.Item().Element(c => ComposeYtdSummary(c, data));
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PayslipData data)
    {
        container.Background(HeaderBg).Padding(12).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(data.CompanyName).FontColor(Colors.White).FontSize(14).Bold();
                if (data.CompanyAddress is not null)
                    col.Item().Text(data.CompanyAddress).FontColor(Colors.White).FontSize(8);
            });
            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().Text("PAYSLIP").FontColor(Colors.White).FontSize(16).Bold();
                col.Item().Text(data.PeriodLabel).FontColor(Colors.White).FontSize(9);
            });
        });
    }

    private static void ComposeEmployeeDetails(IContainer container, PayslipData data)
    {
        container.Border(1).BorderColor(BorderColor).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
                cols.RelativeColumn(2);
                cols.RelativeColumn(3);
            });

            table.Cell().ColumnSpan(4).Background(SectionHeaderBg).Padding(5)
                .Text("Employee Details").Bold().FontSize(9);

            AddDetailRow(table, "Employee Code", data.EmployeeCode, "Department", data.Department);
            AddDetailRow(table, "Employee Name", data.EmployeeName, "Designation", data.Designation);
            AddDetailRow(table, "Pay Period", data.PeriodLabel, "Pay Date",
                data.PayDay.HasValue ? data.PayDay.Value.ToString("dd/MM/yyyy") : "—");
            AddDetailRow(table, "Bank Name", data.BankName ?? "—", "Account No.", data.MaskedBankAccount);
            if (data.IfscCode is not null)
                AddDetailRow(table, "IFSC Code", data.IfscCode, string.Empty, string.Empty);
        });
    }

    private static void ComposeEarningsDeductions(IContainer container, PayslipData data)
    {
        var earnings = data.Components.Where(c => c.IsEarning).ToList();

        container.Border(1).BorderColor(BorderColor).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
                cols.RelativeColumn(4);
                cols.RelativeColumn(2);
                cols.RelativeColumn(2);
            });

            table.Cell().ColumnSpan(3).Background(SectionHeaderBg).Padding(5)
                .Text("Earnings").Bold().FontSize(9);
            table.Cell().ColumnSpan(3).Background(SectionHeaderBg).Padding(5)
                .Text("Deductions").Bold().FontSize(9);

            foreach (string label in new[] { "Component", "Amount (₹)", "YTD (₹)" })
                table.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text(label).Bold().FontSize(8);
            foreach (string label in new[] { "Component", "Amount (₹)", "YTD (₹)" })
                table.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text(label).Bold().FontSize(8);

            var deductions = new List<(string Name, decimal Amount, decimal Ytd)>();
            if (data.EmployeePf > 0m)   deductions.Add(("Employee PF", data.EmployeePf, data.YtdPf));
            if (data.EmployeeEsi > 0m)  deductions.Add(("Employee ESI", data.EmployeeEsi, 0m));
            if (data.PtAmount > 0m)     deductions.Add(("Professional Tax", data.PtAmount, 0m));
            if (data.LwfEmployeeAmount > 0m) deductions.Add(("Labour Welfare Fund", data.LwfEmployeeAmount, 0m));
            if (data.TdsAmount > 0m)    deductions.Add(("Income Tax (TDS)", data.TdsAmount, data.YtdTds));

            int maxRows = Math.Max(earnings.Count, deductions.Count);
            for (int i = 0; i < maxRows; i++)
            {
                if (i < earnings.Count)
                {
                    var e = earnings[i];
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(e.ComponentName).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(e.Amount)).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(e.YtdAmount)).FontSize(8);
                }
                else
                {
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                }

                if (i < deductions.Count)
                {
                    var (name, amount, ytd) = deductions[i];
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(name).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(amount)).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(ytd)).FontSize(8);
                }
                else
                {
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
                }
            }

            decimal totalDeductions = data.EmployeePf + data.EmployeeEsi + data.PtAmount + data.LwfEmployeeAmount + data.TdsAmount;
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Gross Pay").Bold().FontSize(8);
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(data.GrossPay)).Bold().FontSize(8);
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Total Deductions").Bold().FontSize(8);
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatAmount(totalDeductions)).Bold().FontSize(8);
            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(string.Empty);
        });
    }

    private static void ComposeNetPaySummary(IContainer container, PayslipData data)
    {
        container.Background(Color.FromHex("#f8fafc")).Border(1).BorderColor(BorderColor)
            .Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Net Pay").Bold().FontSize(11);
                    col.Item().Text(data.NetPayInWords).FontSize(8).Italic();
                });
                row.ConstantItem(120).AlignRight()
                    .Text($"₹ {data.NetPay:N2}").Bold().FontSize(14);
            });
    }

    private static void ComposeYtdSummary(IContainer container, PayslipData data)
    {
        container.Border(1).BorderColor(BorderColor).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
                cols.RelativeColumn();
            });

            table.Cell().ColumnSpan(4).Background(SectionHeaderBg).Padding(5)
                .Text("Year-to-Date Summary").Bold().FontSize(9);

            AddDetailRow(table, "YTD Gross", FormatAmount(data.YtdGross), "YTD Net Pay", FormatAmount(data.YtdNetPay));
            AddDetailRow(table, "YTD TDS", FormatAmount(data.YtdTds), "YTD PF (Employee)", FormatAmount(data.YtdPf));
        });
    }

    private static void ComposeCTCBreakdown(IContainer container, PayslipData data)
    {
        decimal employerPfInCtc = data.MonthlyCTC - data.GrossPay - data.GratuityAmount;

        container.Border(1).BorderColor(BorderColor).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(5);
                cols.RelativeColumn(2);
            });

            table.Cell().ColumnSpan(2).Background(SectionHeaderBg).Padding(5)
                .Text("CTC Breakdown").Bold().FontSize(9);

            AddCtcRow(table, "Gross Salary (A)", data.GrossPay, bold: false);
            if (data.GratuityAmount > 0m)
                AddCtcRow(table, "Gratuity", data.GratuityAmount, bold: false);
            if (employerPfInCtc > 0m)
                AddCtcRow(table, "Employer PF Contribution", employerPfInCtc, bold: false);
            AddCtcRow(table, "Cost to Company (B)", data.MonthlyCTC, bold: true);
        });
    }

    private static void AddCtcRow(TableDescriptor table, string label, decimal amount, bool bold)
    {
        IContainer labelCell = table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4);
        IContainer amountCell = table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).AlignRight();
        if (bold)
        {
            labelCell.Text(label).FontSize(8).Bold();
            amountCell.Text(FormatAmount(amount)).FontSize(8).Bold();
        }
        else
        {
            labelCell.Text(label).FontSize(8);
            amountCell.Text(FormatAmount(amount)).FontSize(8);
        }
    }

    private static string FormatAmount(decimal amount) =>
        amount == 0m ? "—" : amount.ToString("N2");

    private static void AddDetailRow(TableDescriptor table, string label1, string value1, string label2, string value2)
    {
        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(label1).FontSize(8).FontColor(Colors.Grey.Darken2);
        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(value1).FontSize(8);
        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(label2).FontSize(8).FontColor(Colors.Grey.Darken2);
        table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(4).Text(value2).FontSize(8);
    }
}
