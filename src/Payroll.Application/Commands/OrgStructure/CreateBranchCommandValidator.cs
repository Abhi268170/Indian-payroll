using FluentValidation;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
