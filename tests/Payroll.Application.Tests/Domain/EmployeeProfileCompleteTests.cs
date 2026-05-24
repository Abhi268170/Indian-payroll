using FluentAssertions;
using Payroll.Domain.Enums;
using Xunit;
using EmployeeEntity = Payroll.Domain.Entities.Employee;

namespace Payroll.Application.Tests.DomainEntities;

public class EmployeeProfileCompleteTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static EmployeeEntity NewBaseEmployee() =>
        EmployeeEntity.CreateStep1(
            firstName: "Asha",
            middleName: null,
            lastName: "Nair",
            employeeCode: "EMP100",
            workEmail: "asha@example.com",
            mobileNumber: null,
            gender: Gender.Female,
            dateOfJoining: new DateOnly(2024, 1, 1),
            employmentType: EmploymentType.FullTime,
            isDirector: false,
            enablePortalAccess: false,
            tenantId: TenantId,
            departmentId: Guid.NewGuid(),
            designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(),
            businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 5, 10),
            createdBy: ActorId);

    private static void SetPersonal(EmployeeEntity emp, string? fathersName)
    {
        emp.UpdatePersonalDetails(
            dateOfBirth: emp.DateOfBirth,
            fathersName: fathersName,
            encryptedPAN: null,
            encryptedAadhaar: null,
            personalEmail: null,
            differentlyAbledType: DifferentlyAbledType.None,
            isPWD: false,
            addressLine1: null,
            addressLine2: null,
            city: null,
            residentialState: null,
            pinCode: null,
            updatedBy: ActorId);
    }

    private static void SetBankAccount(EmployeeEntity emp, string? encryptedAccount)
    {
        emp.UpdatePaymentInfo(
            paymentMode: PaymentMode.BankTransfer,
            accountHolderName: "Asha Nair",
            bankName: "ICICI",
            accountType: AccountType.Savings,
            encryptedBankAccount: encryptedAccount,
            encryptedIFSC: "enc-ifsc",
            updatedBy: ActorId);
    }

    [Fact]
    public void RecomputeProfileComplete_AllFieldsPresent_WithStructure_FlipsTrue()
    {
        var emp = NewBaseEmployee();
        SetPersonal(emp, "Ravi Nair");
        SetBankAccount(emp, "enc-account");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);

        emp.ProfileComplete.Should().BeTrue();
    }

    [Fact]
    public void RecomputeProfileComplete_MissingFathersName_StaysFalse()
    {
        var emp = NewBaseEmployee();
        SetBankAccount(emp, "enc-account");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);

        emp.ProfileComplete.Should().BeFalse();
    }

    [Fact]
    public void RecomputeProfileComplete_MissingBankAccount_StaysFalse()
    {
        var emp = NewBaseEmployee();
        SetPersonal(emp, "Ravi Nair");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);

        emp.ProfileComplete.Should().BeFalse();
    }

    [Fact]
    public void RecomputeProfileComplete_MissingActiveStructure_StaysFalse()
    {
        var emp = NewBaseEmployee();
        SetPersonal(emp, "Ravi Nair");
        SetBankAccount(emp, "enc-account");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: false, ActorId);

        emp.ProfileComplete.Should().BeFalse();
    }

    [Fact]
    public void RecomputeProfileComplete_AllPresentThenStructureRemoved_FlipsBackToFalse()
    {
        var emp = NewBaseEmployee();
        SetPersonal(emp, "Ravi Nair");
        SetBankAccount(emp, "enc-account");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);
        emp.ProfileComplete.Should().BeTrue();

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: false, ActorId);
        emp.ProfileComplete.Should().BeFalse();
    }

    [Fact]
    public void RecomputeProfileComplete_IdempotentWhenAlreadyTrue_DoesNotChangeFlag()
    {
        var emp = NewBaseEmployee();
        SetPersonal(emp, "Ravi Nair");
        SetBankAccount(emp, "enc-account");

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);
        DateTimeOffset? updatedAtAfterFirst = emp.UpdatedAt;

        emp.RecomputeProfileComplete(hasActiveSalaryStructure: true, ActorId);

        // Flag unchanged AND UpdatedAt not bumped (no audit churn).
        emp.ProfileComplete.Should().BeTrue();
        emp.UpdatedAt.Should().Be(updatedAtAfterFirst);
    }
}
