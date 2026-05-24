using FluentValidation;
using Payroll.Domain.Constants;

namespace Payroll.Application.Commands.Users;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12);
        RuleFor(x => x.Role).NotEmpty().Must(r => Roles.All.Contains(r) && r != Roles.SuperAdmin)
            .WithMessage($"Role must be one of: {string.Join(", ", Roles.All.Where(r => r != Roles.SuperAdmin))}");
    }
}
