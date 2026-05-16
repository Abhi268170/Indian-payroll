using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.WorkLocations;

internal sealed class CreateWorkLocationCommandValidator : AbstractValidator<CreateWorkLocationCommand>
{
    public CreateWorkLocationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.State)
            .NotEmpty()
            .Must(s => Enum.TryParse<IndianState>(s, out _))
            .WithMessage("State must be a valid Indian state or union territory.");

        RuleFor(x => x.PinCode)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrEmpty(x.PinCode))
            .WithMessage("PinCode must be exactly 6 digits.");
    }
}
