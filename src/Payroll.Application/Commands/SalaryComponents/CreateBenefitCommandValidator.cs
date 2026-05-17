using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateBenefitCommandValidator : AbstractValidator<CreateBenefitCommand>
{
    public CreateBenefitCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameInPayslip).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code must be alphanumeric or underscore.")
            .When(x => !string.IsNullOrEmpty(x.Code));
        RuleFor(x => x.BenefitType)
            .NotEmpty()
            .Must(v => Enum.TryParse<BenefitType>(v, out _))
            .WithMessage("Invalid benefit type.");

        // VPF requires a percentage
        RuleFor(x => x.BenefitPercentage)
            .NotNull().WithMessage("Benefit percentage is required for VPF.")
            .InclusiveBetween(1, 100).WithMessage("VPF percentage must be between 1 and 100.")
            .When(x => x.BenefitType == BenefitType.VPF.ToString());

        // NPS requires government sector flag
        RuleFor(x => x.IsNpsGovernmentSector)
            .NotNull().WithMessage("Government sector flag is required for NPS.")
            .When(x => x.BenefitType == BenefitType.NPS.ToString());
    }
}
