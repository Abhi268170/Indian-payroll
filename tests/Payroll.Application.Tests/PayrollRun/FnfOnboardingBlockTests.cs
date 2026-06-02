using FluentAssertions;
using NSubstitute;
using Payroll.Application.DTOs;
using Payroll.Application.Queries.PayrollRuns;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Engine.Inputs;
using System.Reflection;
using Xunit;

using DomainPayrollRun = Payroll.Domain.Entities.PayrollRun;

namespace Payroll.Application.Tests.PayrollRun;

/// <summary>WI-16: onboarding (bank account / DOB) hard block at FnF approval.</summary>
public class FnfOnboardingBlockTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid RunId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static Employee MakeEmployee(bool withBank, bool withDob = true)
    {
        Employee e = Employee.CreateStep1(
            firstName: "T", middleName: null, lastName: "E", employeeCode: "EMP001",
            workEmail: "t@a.com", mobileNumber: null, gender: Gender.Male,
            dateOfJoining: new DateOnly(2020, 1, 1), employmentType: EmploymentType.FullTime,
            isDirector: false, enablePortalAccess: false, tenantId: TenantId,
            departmentId: Guid.NewGuid(), designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(), businessUnitId: null,
            dateOfBirth: withDob ? new DateOnly(1990, 1, 1) : default, createdBy: ActorId);
        PropertyInfo idProp = typeof(Employee).GetProperty("Id") ?? typeof(Employee).BaseType!.GetProperty("Id")!;
        idProp.SetValue(e, EmployeeId);
        if (withBank)
            e.UpdatePaymentInfo(PaymentMode.BankTransfer, "T E", "HDFC", AccountType.Savings,
                "enc-acct", "enc-ifsc", ActorId);
        return e;
    }

    private static GetPendingTasksHandler Build(Employee emp, decimal grossPay)
    {
        var runRepo = Substitute.For<IPayrollRunRepository>();
        var payrunEmpRepo = Substitute.For<IPayrunEmployeeRepository>();
        var empRepo = Substitute.For<IEmployeeRepository>();

        string snapshot = System.Text.Json.JsonSerializer.Serialize(Minimal());
        DomainPayrollRun run = DomainPayrollRun.CreateFinalSettlement(
            TenantId, new Domain.ValueObjects.PayPeriod(2026, 6),
            new DateOnly(2026, 6, 30), Guid.NewGuid(), snapshot, ActorId);
        runRepo.GetByIdAsync(RunId, Arg.Any<CancellationToken>()).Returns(run);

        PayrunEmployee pe = PayrunEmployee.Create(RunId, EmployeeId, TenantId, 30, ActorId);
        // Non-zero gross so the WI-10 zero-gross block does not fire first.
        pe.UpdateComputedAmounts(50_000m, 45_000m, 42_000m, 2_000m, 0m, 0m,
            1_800m, 1_800m, 0m, 0m, 200m, 1_800m, 0m, 0m, 0m, 0m, 60_000m, ActorId);
        payrunEmpRepo.GetByRunIdAsync(RunId, Arg.Any<CancellationToken>())
            .Returns(new List<PayrunEmployee> { pe });
        empRepo.GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { emp });

        return new GetPendingTasksHandler(runRepo, payrunEmpRepo, empRepo);
    }

    [Fact]
    public async Task Fnf_NoBankAccount_HardBlocks()
    {
        GetPendingTasksHandler h = Build(MakeEmployee(withBank: false), grossPay: 50_000m);
        PendingTasksDto r = await h.Handle(new GetPendingTasksQuery(RunId), CancellationToken.None);
        r.HasAnyHardBlocks.Should().BeTrue();
        r.HardBlocks.Should().Contain(b => b.Reason.Contains("Bank account not configured"));
    }

    [Fact]
    public async Task Fnf_WithBankAccount_NoBlock()
    {
        GetPendingTasksHandler h = Build(MakeEmployee(withBank: true), grossPay: 50_000m);
        PendingTasksDto r = await h.Handle(new GetPendingTasksQuery(RunId), CancellationToken.None);
        r.HardBlocks.Should().NotContain(b => b.Reason.Contains("Bank account"));
    }

    [Fact]
    public async Task Fnf_MissingDob_HardBlocks()
    {
        GetPendingTasksHandler h = Build(MakeEmployee(withBank: true, withDob: false), grossPay: 50_000m);
        PendingTasksDto r = await h.Handle(new GetPendingTasksQuery(RunId), CancellationToken.None);
        r.HardBlocks.Should().Contain(b => b.Reason.Contains("Date of birth missing"));
    }

    private static StatutoryConfig Minimal() => new(
        NewRegimeSlabs: [], SurchargeSlabs: [], StandardDeduction: 75_000m,
        Rebate87ALimit: 700_000m, Rebate87AAmount: 25_000m, CessRate: 0.04m,
        PFWageCap: 15_000m, EPFEmployeeRate: 0.12m, EPSEmployerRate: 0.0833m, EPSCap: 1_250m,
        EpfRestrictEmployerWage: true, EpfConsiderSalaryOnLop: true, EpfProRateRestrictedPfWage: false,
        ESIWageLimit: 21_000m, ESIPWDWageLimit: 25_000m, ESIEmployeeRate: 0.0075m, ESIEmployerRate: 0.0325m,
        PTSlabs: [], LWFStates: [], PFEnabled: true, ESIEnabled: true, PTEnabled: false,
        EpfIncludeEmployerInCtc: false, GratuityIncludedInCtc: false);
}
