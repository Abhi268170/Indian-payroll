using FluentAssertions;
using Payroll.Application.DTOs;
using Payroll.Infrastructure.Services;
using Xunit;

namespace Payroll.Infrastructure.Tests.Services;

public sealed class PayslipPdfGeneratorTests
{
    private static PayslipData BuildSamplePayslipData() => new(
        PayrollRunId: Guid.NewGuid(),
        EmployeeId: Guid.NewGuid(),
        EmployeeCode: "EMP001",
        EmployeeName: "Ravi Kumar",
        Designation: "Software Engineer",
        Department: "Engineering",
        CompanyName: "Acme Technologies Pvt Ltd",
        CompanyAddress: "123 MG Road, Bengaluru",
        PayPeriodYear: 2025,
        PayPeriodMonth: 4,
        PeriodLabel: "April 2025",
        PayDay: new DateOnly(2025, 4, 30),
        MonthlyCTC: 82000m,
        GrossPay: 75000m,
        NetPay: 65484m,
        NetPayInWords: "Indian Rupee Sixty Five Thousand Four Hundred Eighty Four Only",
        EmployeePf: 1800m,
        EmployerPf: 1800m,
        EmployeeEsi: 0m,
        EmployerEsi: 0m,
        PtAmount: 200m,
        LwfEmployeeAmount: 50m,
        TdsAmount: 7516m,
        GratuityAmount: 1442m,
        YtdGross: 75000m,
        YtdNetPay: 65484m,
        YtdTds: 7516m,
        YtdPf: 1800m,
        MaskedBankAccount: "XXXXXXXX1234",
        BankName: "HDFC Bank",
        IfscCode: "HDFC0001234",
        Components: new List<PayslipComponentDto>
        {
            new("BASIC", "Basic Salary", 37500m, 37500m, true),
            new("HRA", "HRA", 18750m, 18750m, true),
            new("SPECIAL", "Special Allowance", 18750m, 18750m, true),
        });

    [Fact]
    public void Generate_ReturnsPdfBytes()
    {
        var generator = new PayslipPdfGenerator();
        PayslipData data = BuildSamplePayslipData();

        byte[] pdf = generator.Generate(data);

        pdf.Should().NotBeEmpty();
    }

    [Fact]
    public void Generate_ReturnsPdfWithValidHeader()
    {
        var generator = new PayslipPdfGenerator();
        PayslipData data = BuildSamplePayslipData();

        byte[] pdf = generator.Generate(data);

        // PDF files start with %PDF
        System.Text.Encoding.Latin1.GetString(pdf[..4]).Should().Be("%PDF");
    }

    [Fact]
    public void Generate_WithNullOptionalFields_DoesNotThrow()
    {
        var generator = new PayslipPdfGenerator();
        PayslipData data = BuildSamplePayslipData() with
        {
            CompanyAddress = null,
            PayDay = null,
            BankName = null,
            IfscCode = null,
        };

        byte[] pdf = generator.Generate(data);

        pdf.Should().NotBeEmpty();
    }
}
