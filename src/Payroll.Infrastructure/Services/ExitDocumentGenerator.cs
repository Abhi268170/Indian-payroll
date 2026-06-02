using Payroll.Application.Interfaces;
using Payroll.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Payroll.Infrastructure.Services;

// WI-31: relieving / experience letter PDF. Mirrors PayslipPdfGenerator's QuestPDF usage.
public sealed class ExitDocumentGenerator : IExitDocumentGenerator
{
    public byte[] GenerateRelievingLetter(Employee employee, EmployeeExit exit, string companyName, string tenureLabel)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Text(companyName).FontSize(16).Bold();
                    col.Item().PaddingTop(2).Text("Relieving & Experience Letter").FontSize(12)
                        .FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(14);
                    col.Item().AlignRight().Text($"Date: {exit.SettlementDate?.ToString("dd MMM yyyy") ?? exit.LastWorkingDay.ToString("dd MMM yyyy")}");
                    col.Item().Text("TO WHOMSOEVER IT MAY CONCERN").Bold();

                    col.Item().Text(
                        $"This is to certify that {employee.FullName} (Employee Code: {employee.EmployeeCode}) " +
                        $"was employed with {companyName} from {employee.DateOfJoining:dd MMM yyyy} to " +
                        $"{exit.LastWorkingDay:dd MMM yyyy}, completing a tenure of {tenureLabel}.");

                    col.Item().Text(
                        $"Their association with the organisation ended on account of {HumanizeReason(exit.Reason)}. " +
                        "They are hereby relieved of all duties and responsibilities with effect from the last working day.");

                    col.Item().Text(
                        "We thank them for their contribution and wish them success in their future endeavours.");

                    col.Item().PaddingTop(30).Text("For " + companyName).Bold();
                    col.Item().PaddingTop(24).Text("Authorised Signatory");
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("This is a system-generated document. ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span($"Ref: {exit.Id}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string HumanizeReason(Domain.Enums.ExitReason reason) => reason switch
    {
        Domain.Enums.ExitReason.ResignedByEmployee => "resignation",
        Domain.Enums.ExitReason.TerminatedByEmployer => "termination",
        Domain.Enums.ExitReason.TerminationByDeath => "demise",
        Domain.Enums.ExitReason.TerminationByDisability => "disability",
        _ => "separation",
    };
}
