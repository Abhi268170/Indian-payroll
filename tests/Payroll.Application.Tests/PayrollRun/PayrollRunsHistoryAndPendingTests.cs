using FluentAssertions;
using NSubstitute;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public class PayrollRunsHistoryAndPendingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static Domain.Entities.PayrollRun MakeRun(PayrollRunType type, PayPeriod period, PayrollRunStatus status, DateOnly? payDay = null)
    {
        var r = Domain.Entities.PayrollRun.Create(TenantId, period, type, payDay, null, 1, ActorId);
        // Force status via reflection-free path: use Approve/MarkPaid where possible.
        if (status == PayrollRunStatus.Approved)
            r.Approve(ActorId);
        else if (status == PayrollRunStatus.Paid)
        {
            r.Approve(ActorId);
            r.RecordPayment(DateOnly.FromDateTime(DateTime.UtcNow), "Bank", "ref", ActorId);
        }
        return r;
    }

    [Fact]
    public async Task GetPayrollHistory_PassesTypeFilterToRepo()
    {
        var runs = new List<Domain.Entities.PayrollRun>
        {
            MakeRun(PayrollRunType.FinalSettlement, new PayPeriod(2026, 6), PayrollRunStatus.Paid),
        };
        var repo = Substitute.For<IPayrollRunRepository>();
        repo.GetHistoryCountAsync(PayrollRunType.FinalSettlement, Arg.Any<CancellationToken>()).Returns(1);
        repo.GetHistoryAsync(0, 25, PayrollRunType.FinalSettlement, Arg.Any<CancellationToken>()).Returns(runs);

        var handler = new GetPayrollHistoryHandler(repo);
        var result = await handler.Handle(new GetPayrollHistoryQuery(1, 25, PayrollRunType.FinalSettlement), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Type.Should().Be("FinalSettlement");
        await repo.Received(1).GetHistoryAsync(0, 25, PayrollRunType.FinalSettlement, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPayrollHistory_NoTypeFilter_PassesNullToRepo()
    {
        var repo = Substitute.For<IPayrollRunRepository>();
        repo.GetHistoryCountAsync(null, Arg.Any<CancellationToken>()).Returns(0);
        repo.GetHistoryAsync(0, 25, null, Arg.Any<CancellationToken>()).Returns(new List<Domain.Entities.PayrollRun>());

        var handler = new GetPayrollHistoryHandler(repo);
        var result = await handler.Handle(new GetPayrollHistoryQuery(1, 25, null), CancellationToken.None);

        result.Total.Should().Be(0);
        await repo.Received(1).GetHistoryAsync(0, 25, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingPayrollRuns_ReturnsCardsWithPrimaryEmployeeLabel_ForFinalSettlement()
    {
        var fsRun = MakeRun(PayrollRunType.FinalSettlement, new PayPeriod(2026, 6), PayrollRunStatus.Draft, new DateOnly(2026, 6, 15));
        var bulkRun = MakeRun(PayrollRunType.BulkFinalSettlement, new PayPeriod(2026, 6), PayrollRunStatus.Draft, new DateOnly(2026, 6, 30));
        var regularRun = MakeRun(PayrollRunType.Regular, new PayPeriod(2026, 6), PayrollRunStatus.Draft, new DateOnly(2026, 6, 30));

        var runRepo = Substitute.For<IPayrollRunRepository>();
        runRepo.ListPendingAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Entities.PayrollRun> { fsRun, bulkRun, regularRun });

        var empId = Guid.NewGuid();
        var exit = EmployeeExit.Create(empId, new DateOnly(2026, 6, 10), ExitReason.ResignedByEmployee,
            ExitSettlementMode.CustomDate, new DateOnly(2026, 6, 15), null, null, ActorId);
        exit.LinkFnfRun(fsRun.Id, ActorId);

        var exitRepo = Substitute.For<IEmployeeExitRepository>();
        exitRepo.GetByFnfRunIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<EmployeeExit> { exit });

        var emp = MakeEmployeeStub(empId, "EMP005", "Asha Nair");
        var empRepo = Substitute.For<IEmployeeRepository>();
        empRepo.GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { emp });

        var handler = new GetPendingPayrollRunsHandler(runRepo, exitRepo, empRepo);
        var cards = await handler.Handle(new GetPendingPayrollRunsQuery(), CancellationToken.None);

        cards.Should().HaveCount(3);
        var fsCard = cards.First(c => c.Type == "FinalSettlement");
        fsCard.PrimaryEmployeeLabel.Should().Be("Asha Nair (EMP005)");
        var bulkCard = cards.First(c => c.Type == "BulkFinalSettlement");
        bulkCard.PrimaryEmployeeLabel.Should().BeNull();
        var regularCard = cards.First(c => c.Type == "Regular");
        regularCard.PrimaryEmployeeLabel.Should().BeNull();
    }

    private static Employee MakeEmployeeStub(Guid id, string code, string name)
    {
        // Employee.Create signature varies; minimal stub via factory if available.
        // We use a lightweight subclass-free approach by leaning on existing factory.
        return EmployeeTestFactory.Create(id, code, name);
    }
}

internal static class EmployeeTestFactory
{
    public static Employee Create(Guid id, string code, string fullName)
    {
        var parts = fullName.Split(' ', 2);
        var first = parts[0];
        var last = parts.Length > 1 ? parts[1] : "";
        var emp = Employee.CreateStep1(
            firstName: first,
            middleName: null,
            lastName: last,
            employeeCode: code,
            workEmail: $"{code.ToLowerInvariant()}@example.com",
            mobileNumber: null,
            gender: Gender.Male,
            dateOfJoining: new DateOnly(2024, 1, 1),
            employmentType: EmploymentType.FullTime,
            isDirector: false,
            enablePortalAccess: false,
            tenantId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(),
            designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(),
            businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 1, 1),
            createdBy: Guid.NewGuid());
        typeof(Employee).BaseType!.GetProperty("Id")!.SetValue(emp, id);
        return emp;
    }
}
