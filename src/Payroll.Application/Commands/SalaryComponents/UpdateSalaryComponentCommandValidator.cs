using FluentValidation;

namespace Payroll.Application.Commands.SalaryComponents;

public sealed class UpdateSalaryComponentCommandValidator : AbstractValidator<UpdateSalaryComponentCommand>
{
    public UpdateSalaryComponentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameInPayslip).NotEmpty().MaximumLength(200);
    }
}
