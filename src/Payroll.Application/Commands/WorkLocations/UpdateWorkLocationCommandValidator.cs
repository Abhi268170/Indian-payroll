using FluentValidation;

namespace Payroll.Application.Commands.WorkLocations;

internal sealed class UpdateWorkLocationCommandValidator : AbstractValidator<UpdateWorkLocationCommand>
{
    public UpdateWorkLocationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.PinCode)
            .Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrEmpty(x.PinCode))
            .WithMessage("PinCode must be exactly 6 digits.");
    }
}
