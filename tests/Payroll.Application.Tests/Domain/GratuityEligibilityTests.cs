using FluentAssertions;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using System.Reflection;
using Xunit;

namespace Payroll.Application.Tests.DomainEntities;

/// <summary>WI-15: Payment of Gratuity Act 5-year (4y 240d) eligibility.</summary>
public class GratuityEligibilityTests
{
    private static Employee WithJoining(DateOnly doj)
    {
        Employee e = Employee.CreateStep1(
            firstName: "T", middleName: null, lastName: "E", employeeCode: "E1",
            workEmail: "t@a.com", mobileNumber: null, gender: Gender.Male,
            dateOfJoining: doj, employmentType: EmploymentType.FullTime,
            isDirector: false, enablePortalAccess: false, tenantId: Guid.NewGuid(),
            departmentId: Guid.NewGuid(), designationId: Guid.NewGuid(),
            workLocationId: Guid.NewGuid(), businessUnitId: null,
            dateOfBirth: new DateOnly(1990, 1, 1), createdBy: Guid.NewGuid());
        return e;
    }

    [Fact]
    public void Exactly5Years_Eligible()
    {
        Employee e = WithJoining(new DateOnly(2021, 6, 1));
        e.IsGratuityEligibleAt(new DateOnly(2026, 6, 1)).Should().BeTrue();
    }

    [Fact]
    public void Over5Years_Eligible()
    {
        Employee e = WithJoining(new DateOnly(2018, 1, 1));
        e.IsGratuityEligibleAt(new DateOnly(2026, 6, 30)).Should().BeTrue();
    }

    [Fact]
    public void FourYears240Days_Eligible()
    {
        // DOJ 2022-01-01; 4th anniversary 2026-01-01; +240 days ≈ 2026-08-29.
        Employee e = WithJoining(new DateOnly(2022, 1, 1));
        DateOnly fourYearsPlus240 = new DateOnly(2026, 1, 1).AddDays(240);
        e.IsGratuityEligibleAt(fourYearsPlus240).Should().BeTrue();
    }

    [Fact]
    public void FourYears239Days_NotEligible()
    {
        Employee e = WithJoining(new DateOnly(2022, 1, 1));
        DateOnly fourYearsPlus239 = new DateOnly(2026, 1, 1).AddDays(239);
        e.IsGratuityEligibleAt(fourYearsPlus239).Should().BeFalse();
    }

    [Fact]
    public void JustUnder4Years_NotEligible()
    {
        Employee e = WithJoining(new DateOnly(2022, 7, 1));
        e.IsGratuityEligibleAt(new DateOnly(2026, 6, 30)).Should().BeFalse();
    }

    [Fact]
    public void OneYear_NotEligible()
    {
        Employee e = WithJoining(new DateOnly(2025, 6, 1));
        e.IsGratuityEligibleAt(new DateOnly(2026, 6, 1)).Should().BeFalse();
    }

    [Fact]
    public void AsOfBeforeJoining_NotEligible()
    {
        Employee e = WithJoining(new DateOnly(2022, 1, 1));
        e.IsGratuityEligibleAt(new DateOnly(2021, 1, 1)).Should().BeFalse();
    }
}
