using FluentAssertions;
using NSubstitute;
using Payroll.Application.Queries.SalaryRevisions;
using Payroll.Application.Services;
using Xunit;

namespace Payroll.Application.Tests.Services;

// WI-018 step 5 — arrear preview query maps the service result to a DTO without persisting.
public class GetSalaryRevisionArrearPreviewTests
{
    [Fact]
    public async Task Handle_MapsServiceResultToDto()
    {
        ISalaryArrearService service = Substitute.For<ISalaryArrearService>();
        Guid revisionId = Guid.NewGuid();
        Guid componentId = Guid.NewGuid();

        service.ComputeForRevisionAsync(revisionId, Arg.Any<CancellationToken>()).Returns(
            new SalaryArrearResult(
                Lines:
                [
                    new ArrearLine(componentId, "BASICSALARY", "Basic Salary", 5600m, IsTaxable: true,
                        ConsiderForEpf: false, ConsiderForEsi: false),
                    new ArrearLine(Guid.NewGuid(), "LTA", "LTA", 1000m, IsTaxable: false,
                        ConsiderForEpf: false, ConsiderForEsi: false),
                ],
                TotalArrear: 6600m,
                TotalTaxableArrear: 5600m,
                ExcludedMonths: [new ArrearMonthExclusion(2026, 4, "No finalised regular run for this month.")]));

        var handler = new GetSalaryRevisionArrearPreviewHandler(service);

        SalaryRevisionArrearPreviewDto dto = await handler.Handle(
            new GetSalaryRevisionArrearPreviewQuery(revisionId), CancellationToken.None);

        dto.TotalArrear.Should().Be(6600m);
        dto.TotalTaxableArrear.Should().Be(5600m);
        dto.Lines.Should().HaveCount(2);
        dto.Lines.Should().Contain(l => l.Code == "BASICSALARY" && l.Name == "Basic Salary" && l.Amount == 5600m && l.IsTaxable);
        dto.Lines.Should().Contain(l => l.Code == "LTA" && !l.IsTaxable);
        dto.ExcludedMonths.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ArrearExcludedMonthDto(2026, 4, "No finalised regular run for this month."));
    }
}
