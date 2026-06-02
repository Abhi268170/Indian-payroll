using System.Reflection;
using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.Employees;
using Payroll.Application.Commands.SalaryRevisions;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;

namespace Payroll.Application.Tests.Employees;

// WI-018 step 3: create + apply salary revision commands.
public class SalaryRevisionCommandTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid ComponentId = Guid.NewGuid();

    private static void SetId<T>(T entity, Guid id)
    {
        PropertyInfo prop = typeof(T).GetProperty("Id") ?? typeof(T).BaseType!.GetProperty("Id")!;
        prop.SetValue(entity, id);
    }

    private static Employee MakeEmployee()
    {
        Employee e = Employee.CreateStep1(
            firstName: "Test", middleName: null, lastName: "Emp",
            employeeCode: "EMP001", workEmail: "t@acme.com", mobileNumber: null,
            gender: Gender.Male, dateOfJoining: new DateOnly(2020, 1, 1),
            employmentType: EmploymentType.FullTime, isDirector: false,
            enablePortalAccess: false, tenantId: TenantId,
            departmentId: Guid.NewGuid(), designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(), businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 1, 1), createdBy: ActorId);
        SetId(e, EmployeeId);
        return e;
    }

    private static EmployeeSalaryStructure MakeActiveStructure(decimal annualCtc)
    {
        EmployeeSalaryStructure s = EmployeeSalaryStructure.Create(
            EmployeeId, TenantId, null, annualCtc, new DateOnly(2025, 4, 1), ActorId);
        SetId(s, Guid.NewGuid());
        return s;
    }

    private sealed class CreateMocks
    {
        public IEmployeeRepository EmployeeRepo = Substitute.For<IEmployeeRepository>();
        public IEmployeeSalaryStructureRepository StructureRepo = Substitute.For<IEmployeeSalaryStructureRepository>();
        public ISalaryRevisionRepository RevisionRepo = Substitute.For<ISalaryRevisionRepository>();
        public ITenantContext Tenant = Substitute.For<ITenantContext>();
        public IUnitOfWork Uow = Substitute.For<IUnitOfWork>();

        public CreateMocks() => Tenant.TenantId.Returns(TenantId);

        public CreateSalaryRevisionHandler Build() => new(EmployeeRepo, StructureRepo, RevisionRepo, Tenant, Uow);
    }

    private static CreateSalaryRevisionCommand CreateCmd() => new(
        EmployeeId: EmployeeId,
        NewAnnualCTC: 720000m,
        EffectiveFromMonth: 3, EffectiveFromYear: 2026,
        PayoutMonth: 6, PayoutYear: 2026,
        SalaryStructureTemplateId: null,
        Overrides: [new ComponentOverrideInput(ComponentId, "Fixed", null, 60000m)],
        Notes: "Annual hike",
        ActorId: ActorId);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_NoActiveStructure_Throws()
    {
        var m = new CreateMocks();
        m.EmployeeRepo.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(MakeEmployee());
        m.StructureRepo.GetActiveAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns((EmployeeSalaryStructure?)null);

        Func<Task> act = () => m.Build().Handle(CreateCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no active salary structure*");
    }

    [Fact]
    public async Task Create_ValidInputs_PersistsPendingRevisionWithPreviousCtcAndOverride()
    {
        var m = new CreateMocks();
        m.EmployeeRepo.GetByIdAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(MakeEmployee());
        m.StructureRepo.GetActiveAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(MakeActiveStructure(600000m));

        SalaryRevision? captured = null;
        await m.RevisionRepo.AddAsync(Arg.Do<SalaryRevision>(r => captured = r), Arg.Any<CancellationToken>());

        await m.Build().Handle(CreateCmd(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(SalaryRevisionStatus.Pending);
        captured.PreviousAnnualCTC.Should().Be(600000m);
        captured.NewAnnualCTC.Should().Be(720000m);
        captured.EffectiveFromMonth.Should().Be(3);
        captured.PayoutMonth.Should().Be(6);
        captured.ComponentOverrides.Should().ContainSingle()
            .Which.SalaryComponentId.Should().Be(ComponentId);
        await m.Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Create_Validator_RejectsBadInputs()
    {
        var v = new CreateSalaryRevisionCommandValidator();

        v.Validate(CreateCmd() with { NewAnnualCTC = 0m }).IsValid.Should().BeFalse();
        v.Validate(CreateCmd() with { EffectiveFromMonth = 8, PayoutMonth = 6 }).IsValid.Should().BeFalse();
        v.Validate(CreateCmd() with { SalaryStructureTemplateId = null, Overrides = [] }).IsValid.Should().BeFalse();
        v.Validate(CreateCmd()).IsValid.Should().BeTrue();
    }

    // ── Apply ───────────────────────────────────────────────────────────────────

    private sealed class ApplyMocks
    {
        public ISalaryRevisionRepository RevisionRepo = Substitute.For<ISalaryRevisionRepository>();
        public IEmployeeSalaryStructureRepository StructureRepo = Substitute.For<IEmployeeSalaryStructureRepository>();
        public ITenantContext Tenant = Substitute.For<ITenantContext>();
        public IUnitOfWork Uow = Substitute.For<IUnitOfWork>();

        public ApplyMocks() => Tenant.TenantId.Returns(TenantId);

        public ApplySalaryRevisionHandler Build() => new(RevisionRepo, StructureRepo, Tenant, Uow);
    }

    private static SalaryRevision MakePendingRevision()
    {
        SalaryRevision r = SalaryRevision.Create(
            EmployeeId, TenantId, 600000m, 720000m,
            effectiveFromMonth: 3, effectiveFromYear: 2026,
            payoutMonth: 6, payoutYear: 2026,
            salaryStructureTemplateId: null, notes: null, createdBy: ActorId);
        SetId(r, Guid.NewGuid());
        r.AddOverride(SalaryRevisionComponentOverride.Create(
            r.Id, ComponentId, ComponentFormulaType.Fixed, null, 60000m, ActorId));
        return r;
    }

    [Fact]
    public async Task Apply_NotFound_Throws()
    {
        var m = new ApplyMocks();
        m.RevisionRepo.GetByIdWithOverridesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((SalaryRevision?)null);

        Func<Task> act = () => m.Build().Handle(new ApplySalaryRevisionCommand(Guid.NewGuid(), ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Apply_AlreadyApplied_Throws()
    {
        var m = new ApplyMocks();
        SalaryRevision r = MakePendingRevision();
        r.Apply(Guid.NewGuid(), ActorId);
        m.RevisionRepo.GetByIdWithOverridesAsync(r.Id, Arg.Any<CancellationToken>()).Returns(r);

        Func<Task> act = () => m.Build().Handle(new ApplySalaryRevisionCommand(r.Id, ActorId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already been applied*");
    }

    [Fact]
    public async Task Apply_ClosesActiveAtEffectiveMinusOne_CreatesBackdatedStructure_AndMarksApplied()
    {
        var m = new ApplyMocks();
        SalaryRevision r = MakePendingRevision();
        m.RevisionRepo.GetByIdWithOverridesAsync(r.Id, Arg.Any<CancellationToken>()).Returns(r);

        EmployeeSalaryStructure active = MakeActiveStructure(600000m);
        m.StructureRepo.GetActiveAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(active);

        EmployeeSalaryStructure? created = null;
        await m.StructureRepo.AddAsync(Arg.Do<EmployeeSalaryStructure>(s => created = s), Arg.Any<CancellationToken>());

        await m.Build().Handle(new ApplySalaryRevisionCommand(r.Id, ActorId), CancellationToken.None);

        // prior structure closed the day before the revision's effective month
        active.EffectiveTo.Should().Be(new DateOnly(2026, 2, 28));
        m.StructureRepo.Received(1).Update(active);

        // new backdated structure
        created.Should().NotBeNull();
        created!.EffectiveFrom.Should().Be(new DateOnly(2026, 3, 1));
        created.AnnualCTC.Should().Be(720000m);
        created.ComponentOverrides.Should().ContainSingle()
            .Which.SalaryComponentId.Should().Be(ComponentId);

        // revision marked applied and linked to the new structure
        r.Status.Should().Be(SalaryRevisionStatus.Applied);
        r.ResultingSalaryStructureId.Should().Be(created.Id);
        m.RevisionRepo.Received(1).Update(r);
        await m.Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
