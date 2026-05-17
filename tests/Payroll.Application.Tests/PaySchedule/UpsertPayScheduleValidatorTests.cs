using FluentAssertions;
using FluentValidation.Results;
using Payroll.Application.Commands.PaySchedule;
using Xunit;

namespace Payroll.Application.Tests.PaySchedule;

public class UpsertPayScheduleValidatorTests
{
    private readonly UpsertPayScheduleCommandValidator _validator = new();

    private static UpsertPayScheduleCommand ValidCommand(
        List<string>? workWeekDays = null,
        string salaryCalcMethod = "ActualDays",
        int? fixedWorkingDays = null,
        string payDateType = "LastDay",
        int? payDateDay = null) =>
        new(
            workWeekDays ?? ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            salaryCalcMethod,
            fixedWorkingDays,
            payDateType,
            payDateDay,
            null,
            null,
            Guid.NewGuid()
        );

    [Fact]
    public void Valid_ActualDays_LastDay_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_FixedDays_SpecificDay_Passes()
    {
        ValidationResult result = _validator.Validate(
            ValidCommand(salaryCalcMethod: "FixedDays", fixedWorkingDays: 26, payDateType: "SpecificDay", payDateDay: 15));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyWorkWeekDays_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(workWeekDays: []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("working day"));
    }

    [Fact]
    public void InvalidDayName_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(workWeekDays: ["Monday", "Freeday"]));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("invalid"));
    }

    [Fact]
    public void InvalidSalaryCalcMethod_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(salaryCalcMethod: "Weekly"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FixedDays_WithoutFixedWorkingDaysPerMonth_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(salaryCalcMethod: "FixedDays", fixedWorkingDays: null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void FixedDays_WithOutOfRangeFixedWorkingDays_Fails(int days)
    {
        ValidationResult result = _validator.Validate(ValidCommand(salaryCalcMethod: "FixedDays", fixedWorkingDays: days));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 1 and 31"));
    }

    [Fact]
    public void ActualDays_WithFixedWorkingDaysProvided_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(salaryCalcMethod: "ActualDays", fixedWorkingDays: 26));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must be empty"));
    }

    [Fact]
    public void InvalidPayDateType_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(payDateType: "BiWeekly"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SpecificDay_WithoutPayDateDay_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(payDateType: "SpecificDay", payDateDay: null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void SpecificDay_WithOutOfRangePayDateDay_Fails(int day)
    {
        ValidationResult result = _validator.Validate(ValidCommand(payDateType: "SpecificDay", payDateDay: day));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 1 and 30"));
    }

    [Fact]
    public void LastDay_WithPayDateDayProvided_Fails()
    {
        ValidationResult result = _validator.Validate(ValidCommand(payDateType: "LastDay", payDateDay: 15));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("must be empty"));
    }
}
