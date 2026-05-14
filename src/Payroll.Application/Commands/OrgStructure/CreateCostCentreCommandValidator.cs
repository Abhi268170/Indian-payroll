using FluentValidation;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateCostCentreCommandValidator : AbstractValidator<CreateCostCentreCommand>
{
    public CreateCostCentreCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}
