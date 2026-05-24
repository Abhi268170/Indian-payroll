using FluentAssertions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.ValueObjects;
using Xunit;

namespace Payroll.Application.Tests.DomainEntities;

public class TenureTests
{
    [Fact]
    public void TenureAt_SameDay_ReturnsZero()
    {
        var emp = MakeEmployee(new DateOnly(2020, 1, 15));
        emp.TenureAt(new DateOnly(2020, 1, 15)).Should().Be(new Tenure(0, 0));
    }

    [Fact]
    public void TenureAt_FullYears_NoMonthsRemainder()
    {
        var emp = MakeEmployee(new DateOnly(2020, 1, 15));
        emp.TenureAt(new DateOnly(2025, 1, 15)).Should().Be(new Tenure(5, 0));
    }

    [Fact]
    public void TenureAt_MidMonth_ComputesPartialMonths()
    {
        var emp = MakeEmployee(new DateOnly(2020, 1, 15));
        emp.TenureAt(new DateOnly(2025, 7, 14)).Should().Be(new Tenure(5, 5));
        emp.TenureAt(new DateOnly(2025, 7, 15)).Should().Be(new Tenure(5, 6));
    }

    [Fact]
    public void YearsForGratuity_RoundsSixMonthsUp()
    {
        new Tenure(4, 5).YearsForGratuity.Should().Be(4);
        new Tenure(4, 6).YearsForGratuity.Should().Be(5);
        new Tenure(4, 11).YearsForGratuity.Should().Be(5);
    }

    [Fact]
    public void TenureAt_BeforeJoining_ReturnsZero()
    {
        var emp = MakeEmployee(new DateOnly(2024, 1, 1));
        emp.TenureAt(new DateOnly(2020, 1, 1)).Should().Be(new Tenure(0, 0));
    }

    private static Employee MakeEmployee(DateOnly doj)
    {
        return Employee.CreateStep1(
            firstName: "T", middleName: null, lastName: "E",
            employeeCode: "TEST", workEmail: "t@e.com", mobileNumber: null,
            gender: Gender.Male, dateOfJoining: doj,
            employmentType: EmploymentType.FullTime, isDirector: false,
            enablePortalAccess: false,
            tenantId: Guid.NewGuid(), departmentId: Guid.NewGuid(),
            designationId: Guid.NewGuid(), workLocationId: Guid.NewGuid(),
            businessUnitId: null, dateOfBirth: new DateOnly(1990, 1, 1),
            createdBy: Guid.NewGuid());
    }
}
