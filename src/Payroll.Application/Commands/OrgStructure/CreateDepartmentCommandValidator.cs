using FluentValidation;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50).When(x => x.Code is not null);
    }
}
