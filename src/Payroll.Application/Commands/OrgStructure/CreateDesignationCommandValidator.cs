using FluentValidation;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateDesignationCommandValidator : AbstractValidator<CreateDesignationCommand>
{
    public CreateDesignationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
