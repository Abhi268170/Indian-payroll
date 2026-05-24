using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateDeductionCommandValidator : AbstractValidator<CreateDeductionCommand>
{
    public CreateDeductionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameInPayslip).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code must be alphanumeric or underscore.")
            .When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.DeductionFrequency)
            .NotEmpty()
            .Must(v => Enum.TryParse<DeductionFrequency>(v, out _))
            .WithMessage("Invalid deduction frequency.");
    }
}
