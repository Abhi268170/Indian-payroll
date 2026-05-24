using FluentValidation;
using Payroll.Domain.Enums;

namespace Payroll.Application.Commands.SalaryStructureTemplates;

public sealed class UpdateSalaryStructureTemplateCommandValidator
    : AbstractValidator<UpdateSalaryStructureTemplateCommand>
{
    public UpdateSalaryStructureTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);

        RuleFor(x => x.Components)
            .NotEmpty().WithMessage("A salary structure must have at least one component.")
            .Must(c => c.Count(x => x.FormulaType == ComponentFormulaType.ResidualCTC.ToString()) == 1)
            .WithMessage("Exactly one Fixed Allowance (Residual CTC) component is required.")
            .Must(c => c.Select(x => x.ComponentId).Distinct().Count() == c.Count)
            .WithMessage("Duplicate components are not allowed.")
            .Must(c =>
            {
                decimal sum = c.Where(x => x.FormulaType == ComponentFormulaType.PercentOfCTC.ToString() && x.Percentage.HasValue)
                               .Sum(x => x.Percentage!.Value);
                return sum <= 100;
            })
            .WithMessage("Sum of PercentOfCTC percentages cannot exceed 100%.");
    }
}
