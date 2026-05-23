using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class CreateEarningCommandValidator : AbstractValidator<CreateEarningCommand>
{
    private static readonly HashSet<string> AllowedEarningTypes =
        Enum.GetValues<EarningType>()
            .Where(t => t != EarningType.FixedAllowance)
            .Select(t => t.ToString())
            .ToHashSet();

    public CreateEarningCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameInPayslip).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code)
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Code must be alphanumeric or underscore.")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.EarningType)
            .NotEmpty()
            .Must(v => AllowedEarningTypes.Contains(v))
            .WithMessage("Invalid or reserved earning type.");

        RuleFor(x => x.PayType)
            .NotEmpty()
            .Must(v => Enum.TryParse<PayType>(v, out _))
            .WithMessage("Invalid pay type.");

        RuleFor(x => x.FormulaType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ComponentFormulaType>(v, out var ft) && ft != ComponentFormulaType.ResidualCTC)
            .WithMessage("Invalid formula type.");

        // One-time components carry no preset amount — the operator types it
        // in the payroll run drawer. For recurring Fixed earnings the amount
        // is still required up front.
        RuleFor(x => x.FixedAmount)
            .NotNull().WithMessage("Fixed amount is required for Fixed formula.")
            .GreaterThan(0).WithMessage("Fixed amount must be positive.")
            .When(x => x.FormulaType == ComponentFormulaType.Fixed.ToString() && !x.IsOneTime);

        RuleFor(x => x.FixedAmount)
            .Null().WithMessage("Fixed amount must be empty for percentage-based formula.")
            .When(x => x.FormulaType != ComponentFormulaType.Fixed.ToString());

        RuleFor(x => x.Percentage)
            .NotNull().WithMessage("Percentage is required for percentage-based formula.")
            .InclusiveBetween(0, 100).WithMessage("Percentage must be between 0 and 100.")
            .When(x => x.FormulaType != ComponentFormulaType.Fixed.ToString()
                       && x.FormulaType != ComponentFormulaType.ResidualCTC.ToString());

        RuleFor(x => x.Percentage)
            .Null().WithMessage("Percentage must be empty for Fixed formula.")
            .When(x => x.FormulaType == ComponentFormulaType.Fixed.ToString());

        RuleFor(x => x.EpfInclusionRule)
            .NotEmpty()
            .Must(v => Enum.TryParse<EpfInclusionRule>(v, out _))
            .WithMessage("Invalid EPF inclusion rule.")
            .When(x => x.ConsiderForEpf);
    }
}
