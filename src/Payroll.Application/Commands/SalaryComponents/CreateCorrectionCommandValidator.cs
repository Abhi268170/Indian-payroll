using FluentValidation;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateCorrectionCommandValidator : AbstractValidator<CreateCorrectionCommand>
{
    public CreateCorrectionCommandValidator()
    {
        RuleFor(x => x.CorrectionName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code must be alphanumeric or underscore.")
            .When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.ForCorrectionOfComponentId).NotEmpty();
    }
}
