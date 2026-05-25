using FluentAssertions;
using Payroll.Application.Services;
using Payroll.Domain.Entities;
using Xunit;

namespace Payroll.Application.Tests.Services;

public class PayrollCostCalculatorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();

    private static PayrunEmployee Make(
        decimal gross,
        decimal net,
        decimal employerPf,
        decimal eps,
        decimal employerEsi,
        decimal lwfEmployer,
        decimal gratuity,
        decimal tds,
        decimal pt)
    {
        var emp = PayrunEmployee.Create(RunId, Guid.NewGuid(), TenantId, 31, Actor);
        emp.UpdateComputedAmounts(
            grossPay: gross,
            taxableGrossPay: gross,
            netPay: net,
            taxesAmount: tds + pt,
            benefitsAmount: employerPf + employerEsi,
            reimbursementsAmount: 0m,
            employeePf: 0m,
            employerPf: employerPf,
            employeeEsi: 0m,
            employerEsi: employerEsi,
            ptAmount: pt,
            tdsAmount: tds,
            lwfEmployeeAmount: 0m,
            lwfEmployerAmount: lwfEmployer,
            gratuityAmount: gratuity,
            epsAmount: eps,
            monthlyCTC: 0m,
            actorId: Actor);
        return emp;
    }

    [Fact]
    public void Calculate_EmptyList_ReturnsAllZeros()
    {
        var sut = new PayrollCostCalculator();
        var snap = sut.Calculate([]);
        snap.PayrollCost.Should().Be(0m);
        snap.TotalGross.Should().Be(0m);
        snap.EmployeeCount.Should().Be(0);
    }

    [Fact]
    public void Calculate_SingleEmployee_PayrollCostIsSumOfSixComponents()
    {
        // Gross + employerPf + eps + employerEsi + lwfEmployer + gratuity
        // 50000 + 1800     + 1250 + 1625        + 75          + 2403   = 57153
        var emp = Make(
            gross: 50_000m, net: 45_000m,
            employerPf: 1_800m, eps: 1_250m,
            employerEsi: 1_625m, lwfEmployer: 75m,
            gratuity: 2_403m, tds: 2_500m, pt: 200m);

        var snap = new PayrollCostCalculator().Calculate([emp]);

        snap.PayrollCost.Should().Be(57_153m);
        snap.TotalGross.Should().Be(50_000m);
        snap.TotalNet.Should().Be(45_000m);
        snap.TotalEmployerPf.Should().Be(1_800m);
        snap.TotalEmployerEps.Should().Be(1_250m);
        snap.TotalEmployerEsi.Should().Be(1_625m);
        snap.TotalLwfEmployer.Should().Be(75m);
        snap.TotalGratuity.Should().Be(2_403m);
        snap.TotalTds.Should().Be(2_500m);
        snap.TotalPt.Should().Be(200m);
        snap.EmployeeCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_MultipleEmployees_SumsLinearly()
    {
        var e1 = Make(50_000m, 45_000m, 1_800m, 1_250m, 1_625m, 75m, 2_403m, 2_500m, 200m);
        var e2 = Make(30_000m, 27_000m, 1_080m, 750m, 975m, 75m, 1_442m, 1_500m, 200m);

        var snap = new PayrollCostCalculator().Calculate([e1, e2]);

        snap.TotalGross.Should().Be(80_000m);
        snap.PayrollCost.Should().Be(57_153m + 34_322m);
        snap.EmployeeCount.Should().Be(2);
    }

    // Regression for Finding #3: prior 3-component formula
    // (gross + employerPf + employerEsi) dropped EPS, LWF, gratuity.
    // This test pins the canonical formula so SetLop, BulkImport*,
    // and Initiate all stay aligned.
    [Fact]
    public void Calculate_PayrollCost_DoesNotMatchDeprecatedThreeComponentFormula()
    {
        var emp = Make(50_000m, 45_000m, 1_800m, 1_250m, 1_625m, 75m, 2_403m, 2_500m, 200m);
        var snap = new PayrollCostCalculator().Calculate([emp]);

        decimal deprecated = 50_000m + 1_800m + 1_625m; // = 53_425
        snap.PayrollCost.Should().NotBe(deprecated);
        snap.PayrollCost.Should().Be(57_153m);
    }
}
