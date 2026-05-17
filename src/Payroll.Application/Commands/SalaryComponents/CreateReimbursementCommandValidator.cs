using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateReimbursementCommandValidator : AbstractValidator<CreateReimbursementCommand>
{
    public CreateReimbursementCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameInPayslip).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code must be alphanumeric or underscore.")
            .When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.ReimbursementType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ReimbursementType>(v, out _))
            .WithMessage("Invalid reimbursement type.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
        RuleFor(x => x.UnclaimedHandling)
            .NotEmpty()
            .Must(v => Enum.TryParse<UnclaimedReimbursementHandling>(v, out _))
            .WithMessage("Invalid unclaimed handling option.");
    }
}
