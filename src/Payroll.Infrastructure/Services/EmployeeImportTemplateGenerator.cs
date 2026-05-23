using ClosedXML.Excel;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Services;

public sealed class EmployeeImportTemplateGenerator : IEmployeeImportTemplateGenerator
{
    private enum ColumnKind { Required, Secure, Optional }

    private static readonly (string Header, ColumnKind Kind, string Hint)[] Columns =
    [
        ("EmployeeNumber",        ColumnKind.Optional,  "e.g. EMP001 — leave blank to auto-generate"),
        ("FirstName",             ColumnKind.Required,  "Required"),
        ("MiddleName",            ColumnKind.Optional,  "Optional"),
        ("LastName",              ColumnKind.Required,  "Required"),
        ("Gender",                ColumnKind.Required,  "Male / Female / Other"),
        ("DateOfJoining",         ColumnKind.Required,  "YYYY-MM-DD"),
        ("DateOfBirth",           ColumnKind.Required,  "YYYY-MM-DD"),
        ("WorkEmail",             ColumnKind.Required,  "Required — must be unique"),
        ("PersonalEmail",         ColumnKind.Optional,  "Optional"),
        ("MobileNumber",          ColumnKind.Optional,  "10 digits, no spaces"),
        ("FathersName",           ColumnKind.Optional,  "Optional"),
        ("AddressLine1",          ColumnKind.Optional,  "Optional"),
        ("AddressLine2",          ColumnKind.Optional,  "Optional"),
        ("City",                  ColumnKind.Optional,  "Optional"),
        ("State",                 ColumnKind.Optional,  "e.g. KA, MH, TN"),
        ("PinCode",               ColumnKind.Optional,  "6 digits"),
        ("PAN",                   ColumnKind.Secure,    "Format: AAAAA9999A — stored encrypted"),
        ("Aadhaar",               ColumnKind.Secure,    "12 digits — stored encrypted"),
        ("Department",            ColumnKind.Required,  "Must exist in Settings"),
        ("Designation",           ColumnKind.Required,  "Must exist in Settings"),
        ("WorkLocation",          ColumnKind.Required,  "Must exist in Settings"),
        ("EmploymentType",        ColumnKind.Required,  "FullTime / PartTime / Contract / Intern"),
        ("PaymentMode",           ColumnKind.Optional,  "BankTransfer / DirectDeposit / Cheque / Cash"),
        ("BankAccountHolderName", ColumnKind.Optional,  "Optional"),
        ("BankName",              ColumnKind.Optional,  "Optional"),
        ("BankAccountNumber",     ColumnKind.Secure,    "Stored encrypted"),
        ("IFSCCode",              ColumnKind.Secure,    "11 characters — stored encrypted"),
        ("BankAccountType",       ColumnKind.Optional,  "Savings / Current"),
        ("EpfEnabled",            ColumnKind.Optional,  "true / false — default true"),
        ("EsiEnabled",            ColumnKind.Optional,  "true / false — default true"),
        ("PtEnabled",             ColumnKind.Optional,  "true / false — default true"),
        ("LwfEnabled",            ColumnKind.Optional,  "true / false — default true"),
        ("UAN",                   ColumnKind.Optional,  "12 digits"),
        ("ESICNumber",            ColumnKind.Optional,  "Optional"),
        ("AnnualCTC",             ColumnKind.Optional,  "Annual CTC in INR — e.g. 1200000"),
        ("SalaryStructureTemplate", ColumnKind.Optional, "Template name — must exist in Settings"),
    ];

    private static readonly XLColor RequiredBg = XLColor.FromHtml("#FECACA"); // red-200
    private static readonly XLColor SecureBg   = XLColor.FromHtml("#FEF08A"); // yellow-300
    private static readonly XLColor HeaderText  = XLColor.FromHtml("#0f172a");
    private static readonly XLColor HintText    = XLColor.FromHtml("#64748b");

    public byte[] Generate()
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet ws = workbook.AddWorksheet("Employees");
        IXLWorksheet legend = workbook.AddWorksheet("Instructions");

        BuildDataSheet(ws);
        BuildLegendSheet(legend);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void BuildDataSheet(IXLWorksheet ws)
    {
        ws.SheetView.FreezeRows(2);

        for (int i = 0; i < Columns.Length; i++)
        {
            int col = i + 1;
            (string header, ColumnKind kind, string hint) = Columns[i];

            // Header row
            IXLCell headerCell = ws.Cell(1, col);
            headerCell.Value = header;
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Font.FontColor = HeaderText;
            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            headerCell.Style.Border.BottomBorderColor = XLColor.FromHtml("#e2e8f0");

            switch (kind)
            {
                case ColumnKind.Required:
                    headerCell.Style.Fill.BackgroundColor = RequiredBg;
                    break;
                case ColumnKind.Secure:
                    headerCell.Style.Fill.BackgroundColor = SecureBg;
                    break;
            }

            // Hint row
            IXLCell hintCell = ws.Cell(2, col);
            hintCell.Value = hint;
            hintCell.Style.Font.Italic = true;
            hintCell.Style.Font.FontSize = 9;
            hintCell.Style.Font.FontColor = HintText;
            hintCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");

            // Example data row
            string? example = ExampleValue(header);
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

        ws.Cell(4, 1).Style.Fill.BackgroundColor = SecureBg;
        ws.Cell(4, 2).Value = "Sensitive — stored AES-256 encrypted at rest";

        ws.Cell(5, 2).Value = "Optional — leave blank to skip";

        ws.Cell(7, 1).Value = "Notes";
        ws.Cell(7, 1).Style.Font.Bold = true;
        ws.Cell(8, 1).Value = "• Department, Designation, and WorkLocation must already exist in Settings before importing.";
        ws.Cell(9, 1).Value = "• All errors in a file are shown together before import — fix all errors, then re-upload.";
        ws.Cell(10, 1).Value = "• AnnualCTC is optional. If provided, a salary structure is created for the employee.";
        ws.Cell(11, 1).Value = "• Maximum 1000 rows per file.";

        ws.Column(1).Width = 4;
        ws.Column(2).Width = 60;
        ws.Rows(8, 11).Style.Font.FontSize = 10;
    }

    private static string? ExampleValue(string header) => header switch
    {
        "EmployeeNumber"          => "EMP001",
        "FirstName"               => "Priya",
        "MiddleName"              => "",
        "LastName"                => "Sharma",
        "Gender"                  => "Female",
        "DateOfJoining"           => "2024-01-15",
        "DateOfBirth"             => "1995-06-20",
        "WorkEmail"               => "priya.sharma@acme.com",
        "PersonalEmail"           => "priya@gmail.com",
        "MobileNumber"            => "9876543210",
        "FathersName"             => "Ramesh Sharma",
        "AddressLine1"            => "12 MG Road",
        "City"                    => "Bengaluru",
        "State"                   => "KA",
        "PinCode"                 => "560001",
        "PAN"                     => "ABCDE1234F",
        "Aadhaar"                 => "123456789012",
        "Department"              => "Engineering",
        "Designation"             => "Software Engineer",
        "WorkLocation"            => "Bangalore Office",
        "EmploymentType"          => "FullTime",
        "PaymentMode"             => "BankTransfer",
        "BankAccountHolderName"   => "Priya Sharma",
        "BankName"                => "HDFC Bank",
        "BankAccountNumber"       => "12345678901",
        "IFSCCode"                => "HDFC0001234",
        "BankAccountType"         => "Savings",
        "EpfEnabled"              => "true",
        "EsiEnabled"              => "true",
        "PtEnabled"               => "true",
        "LwfEnabled"              => "true",
        "AnnualCTC"               => "1200000",
        "SalaryStructureTemplate" => "Standard",
        _                         => null,
    };
}
