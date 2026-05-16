using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.PaySchedule;

public sealed class UpsertPayScheduleCommandValidator : AbstractValidator<UpsertPayScheduleCommand>
{
    private static readonly HashSet<string> ValidDayNames =
        Enum.GetValues<WorkWeekDay>()
            .Where(d => d != WorkWeekDay.None && d != WorkWeekDay.StandardFiveDay)
            .Select(d => d.ToString())
            .ToHashSet();

    public UpsertPayScheduleCommandValidator()
    {
        RuleFor(x => x.WorkWeekDays)
            .NotEmpty().WithMessage("At least one working day must be selected.")
            .Must(days => days.All(d => ValidDayNames.Contains(d)))
            .WithMessage("One or more work week day values are invalid.")
            .Must(days => days.Count != 0)
            .WithMessage("At least one working day must be selected.");

        RuleFor(x => x.SalaryCalculationMethod)
            .NotEmpty()
            .Must(v => Enum.TryParse<SalaryCalculationMethod>(v, out _))
            .WithMessage("Salary calculation method is invalid.");

        RuleFor(x => x.FixedWorkingDaysPerMonth)
            .InclusiveBetween(1, 31)
            .WithMessage("Fixed working days must be between 1 and 31.")
            .When(x => x.SalaryCalculationMethod == SalaryCalculationMethod.FixedDays.ToString());

        RuleFor(x => x.FixedWorkingDaysPerMonth)
            .NotNull()
            .WithMessage("Fixed working days per month is required when method is FixedDays.")
            .When(x => x.SalaryCalculationMethod == SalaryCalculationMethod.FixedDays.ToString());

        RuleFor(x => x.FixedWorkingDaysPerMonth)
            .Null()
            .WithMessage("Fixed working days per month must be empty when method is ActualDays.")
            .When(x => x.SalaryCalculationMethod == SalaryCalculationMethod.ActualDays.ToString());

        RuleFor(x => x.PayDateType)
            .NotEmpty()
            .Must(v => Enum.TryParse<PayDateType>(v, out _))
            .WithMessage("Pay date type is invalid.");

        RuleFor(x => x.PayDateDay)
            .NotNull()
            .WithMessage("Pay date day is required when pay date type is SpecificDay.")
            .When(x => x.PayDateType == PayDateType.SpecificDay.ToString());

        RuleFor(x => x.PayDateDay)
            .InclusiveBetween(1, 30)
            .WithMessage("Pay date day must be between 1 and 30.")
            .When(x => x.PayDateType == PayDateType.SpecificDay.ToString() && x.PayDateDay.HasValue);

        RuleFor(x => x.PayDateDay)
            .Null()
            .WithMessage("Pay date day must be empty when pay date type is LastDay.")
            .When(x => x.PayDateType == PayDateType.LastDay.ToString());
    }
}
