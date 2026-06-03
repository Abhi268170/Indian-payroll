using System.Reflection;
using FluentAssertions;
using Payroll.Application.Commands.SalaryRevisions;
using Payroll.Application.Interfaces;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Xunit;

namespace Payroll.Application.Tests.Services;

// WI-018 step 8 — bulk-import validation/resolution matrix.
public class SalaryRevisionImportProcessorTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmpId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();

    private static Employee MakeEmployee(string code)
    {
        Employee e = Employee.CreateStep1(
            "Test", null, "Emp", code, $"{code}@acme.com", null,
            Gender.Male, new DateOnly(2020, 1, 1), EmploymentType.FullTime, false, false,
            TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(1990, 1, 1), ActorId);
        typeof(Employee).GetProperty("Id")!.SetValue(e, EmpId);
        return e;
    }

    private static SalaryRevisionImportRow Row(
        int n = 3, string? code = "EMP001", string? ctc = "1320000",
        string? effM = "3", string? effY = "2026", string? poM = "5", string? poY = "2026",
        string? tmpl = "Standard", string? notes = "hike") =>
        new(n, code, ctc, effM, effY, poM, poY, tmpl, notes);

    private static (Dictionary<string, Employee>, Dictionary<string, Guid>, Dictionary<Guid, decimal>, Dictionary<Guid, HashSet<(int, int)>>) Ctx(
        bool withActive = true, params (int, int)[] existingPayouts)
    {
        var emps = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase) { ["EMP001"] = MakeEmployee("EMP001") };
        var tmpls = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase) { ["Standard"] = TemplateId };
        var ctc = new Dictionary<Guid, decimal>();
        if (withActive) ctc[EmpId] = 1_200_000m;
        var existing = new Dictionary<Guid, HashSet<(int, int)>> { [EmpId] = existingPayouts.ToHashSet() };
        return (emps, tmpls, ctc, existing);
    }

    [Fact]
    public void ValidRow_Resolves()
    {
        var (e, t, c, x) = Ctx();
        var (valid, skipped, errors) = SalaryRevisionImportProcessor.Process([Row()], e, t, c, x, overwrite: false);

        errors.Should().BeEmpty();
        skipped.Should().BeEmpty();
        valid.Should().ContainSingle();
        valid[0].PreviousAnnualCTC.Should().Be(1_200_000m);
        valid[0].NewAnnualCTC.Should().Be(1_320_000m);
        valid[0].TemplateId.Should().Be(TemplateId);
        valid[0].PayoutMonth.Should().Be(5);
    }

    [Fact]
    public void UnknownEmployee_Errors()
    {
        var (e, t, c, x) = Ctx();
        var (valid, _, errors) = SalaryRevisionImportProcessor.Process([Row(code: "NOPE")], e, t, c, x, false);
        valid.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("not found");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void BadCtc_Errors(string ctc)
    {
        var (e, t, c, x) = Ctx();
        var (valid, _, errors) = SalaryRevisionImportProcessor.Process([Row(ctc: ctc)], e, t, c, x, false);
        valid.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("NewAnnualCTC");
    }

    [Fact]
    public void EffectiveAfterPayout_Errors()
    {
        var (e, t, c, x) = Ctx();
        var (valid, _, errors) = SalaryRevisionImportProcessor.Process(
            [Row(effM: "8", effY: "2026", poM: "5", poY: "2026")], e, t, c, x, false);
        valid.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("on or before the payout");
    }

    [Fact]
    public void UnknownTemplate_Errors()
    {
        var (e, t, c, x) = Ctx();
        var (valid, _, errors) = SalaryRevisionImportProcessor.Process([Row(tmpl: "Ghost")], e, t, c, x, false);
        valid.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("Template");
    }

    [Fact]
    public void NoActiveStructure_Errors()
    {
        var (e, t, c, x) = Ctx(withActive: false);
        var (valid, _, errors) = SalaryRevisionImportProcessor.Process([Row()], e, t, c, x, false);
        valid.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("no active salary structure");
    }

    [Fact]
    public void Duplicate_SkippedWhenNotOverwriting()
    {
        var (e, t, c, x) = Ctx(existingPayouts: (5, 2026));
        var (valid, skipped, errors) = SalaryRevisionImportProcessor.Process([Row()], e, t, c, x, overwrite: false);
        valid.Should().BeEmpty();
        skipped.Should().ContainSingle();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Duplicate_ResolvedAsOverwriteWhenOverwriting()
    {
        var (e, t, c, x) = Ctx(existingPayouts: (5, 2026));
        var (valid, skipped, _) = SalaryRevisionImportProcessor.Process([Row()], e, t, c, x, overwrite: true);
        skipped.Should().BeEmpty();
        valid.Should().ContainSingle().Which.IsDuplicate.Should().BeTrue();
    }
}
