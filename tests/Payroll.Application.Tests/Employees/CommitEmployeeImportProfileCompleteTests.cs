using FluentAssertions;
using NSubstitute;
using Payroll.Application.Commands.Employees;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Xunit;
using EmployeeEntity = Payroll.Domain.Entities.Employee;

namespace Payroll.Application.Tests.Employees;

// Regression coverage for the audit follow-up: import path must flip
// Employee.ProfileComplete the same way the interactive handlers do. Without
// this, a successful CSV import leaves first-employee onboarding step stuck
// at "incomplete".
public class CommitEmployeeImportProfileCompleteTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid DeptId = Guid.NewGuid();
    private static readonly Guid DesigId = Guid.NewGuid();
    private static readonly Guid WlId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();

    private static EmployeeImportRow ValidNewRow() => new(
        RowNumber: 1,
        EmployeeNumber: "EMP-NEW",
        FirstName: "Asha",
        MiddleName: null,
        LastName: "Nair",
        Gender: "Female",
        DateOfJoining: "2024-01-01",
        DateOfBirth: "1990-05-10",
        WorkEmail: "asha@example.com",
        PersonalEmail: null,
        MobileNumber: null,
        FathersName: "Ravi Nair",
        AddressLine1: null, AddressLine2: null, City: null, State: null, PinCode: null,
        PAN: null, Aadhaar: null,
        Department: "Engineering",
        Designation: "Software Engineer",
        WorkLocation: "Head Office",
        EmploymentType: "FullTime",
        PaymentMode: "BankTransfer",
        BankAccountHolderName: "Asha Nair",
        BankName: "ICICI",
        BankAccountNumber: "1234567890",
        IFSCCode: "ICIC0001234",
        BankAccountType: "Savings",
        EpfEnabled: null, EsiEnabled: null, PtEnabled: null, LwfEnabled: null,
        UAN: null, ESICNumber: null,
        AnnualCTC: "1200000",
        SalaryStructureTemplate: "Standard");

    [Fact]
    public async Task ImportNewRow_WithCompleteData_FlipsProfileComplete()
    {
        EmployeeEntity? captured = null;

        var parser = Substitute.For<IEmployeeImportParser>();
        parser.Parse(Arg.Any<Stream>()).Returns(new[] { ValidNewRow() });

        var employeeRepo = Substitute.For<IEmployeeRepository>();
        employeeRepo.GetExistingCodesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        employeeRepo.GetExistingEmailsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        employeeRepo.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EmployeeEntity>());
        employeeRepo.AddAsync(Arg.Do<EmployeeEntity>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var salaryRepo = Substitute.For<IEmployeeSalaryStructureRepository>();

        var deptRepo = Substitute.For<IDepartmentRepository>();
        deptRepo.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Department.Create("Engineering", null, null, ActorId) }
                .Select(d => { typeof(AuditableEntity).GetProperty("Id")!.SetValue(d, DeptId); return d; })
                .ToList()
                .AsReadOnly() as IReadOnlyList<Department>);

        var desigRepo = Substitute.For<IDesignationRepository>();
        desigRepo.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Designation.Create("Software Engineer", ActorId) }
                .Select(d => { typeof(AuditableEntity).GetProperty("Id")!.SetValue(d, DesigId); return d; })
                .ToList()
                .AsReadOnly() as IReadOnlyList<Designation>);

        var wlRepo = Substitute.For<IWorkLocationRepository>();
        wlRepo.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { WorkLocation.Create("Head Office", IndianState.Karnataka, null, null, null, null, ActorId) }
                .Select(w => { typeof(AuditableEntity).GetProperty("Id")!.SetValue(w, WlId); return w; })
                .ToList()
                .AsReadOnly() as IReadOnlyList<WorkLocation>);

        var templateRepo = Substitute.For<ISalaryStructureTemplateRepository>();
        templateRepo.ListByTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<SalaryStructureTemplate>
            {
                CreateTemplate("Standard", TemplateId),
            });

        var encryption = Substitute.For<IEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(c => $"enc:{c.Arg<string>()}");

        var tenant = Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);

        var uow = Substitute.For<IUnitOfWork>();

        var handler = new CommitEmployeeImportHandler(
            parser, employeeRepo, salaryRepo, deptRepo, desigRepo, wlRepo, templateRepo,
            encryption, tenant, uow);

        using var stream = new MemoryStream();
        await handler.Handle(new CommitEmployeeImportCommand(stream, OverwriteExisting: false, ActorId), CancellationToken.None);

        captured.Should().NotBeNull("the handler should call AddAsync for new rows");
        captured!.ProfileComplete.Should().BeTrue("all required fields + a salary structure were provided in the row");
    }

    private static SalaryStructureTemplate CreateTemplate(string name, Guid id)
    {
        var t = SalaryStructureTemplate.Create(name, null, TenantId, ActorId);
        typeof(AuditableEntity).GetProperty("Id")!.SetValue(t, id);
        return t;
    }
}
