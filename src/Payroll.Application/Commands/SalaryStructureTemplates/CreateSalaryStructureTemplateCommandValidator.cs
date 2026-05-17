using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed class CreateSalaryStructureTemplateCommandValidator
    : AbstractValidator<CreateSalaryStructureTemplateCommand>
{
    public CreateSalaryStructureTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.Components)
            .NotEmpty().WithMessage("A salary structure must have at least one component.")
            .Must(HaveExactlyOneResidualComponent)
            .WithMessage("Exactly one Fixed Allowance (Residual CTC) component is required.")
            .Must(HaveNoDuplicateComponents)
            .WithMessage("Duplicate components are not allowed in a salary structure.")
            .Must(HaveValidPercentageSum)
            .WithMessage("Sum of PercentOfCTC component percentages cannot exceed 100%.");

        RuleForEach(x => x.Components).ChildRules(c =>
        {
            c.RuleFor(x => x.ComponentId).NotEmpty();
            c.RuleFor(x => x.FormulaType)
             .NotEmpty()
             .Must(v => Enum.TryParse<ComponentFormulaType>(v, out _))
             .WithMessage("Invalid formula type.");
            c.RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }

    private static bool HaveExactlyOneResidualComponent(IReadOnlyList<TemplateComponentInput> components) =>
        components.Count(c => c.FormulaType == ComponentFormulaType.ResidualCTC.ToString()) == 1;

    private static bool HaveNoDuplicateComponents(IReadOnlyList<TemplateComponentInput> components) =>
        components.Select(c => c.ComponentId).Distinct().Count() == components.Count;

    private static bool HaveValidPercentageSum(IReadOnlyList<TemplateComponentInput> components)
    {
        decimal sum = components
            .Where(c => c.FormulaType == ComponentFormulaType.PercentOfCTC.ToString()
                        && c.Percentage.HasValue)
            .Sum(c => c.Percentage!.Value);
        return sum <= 100;
    }
}
