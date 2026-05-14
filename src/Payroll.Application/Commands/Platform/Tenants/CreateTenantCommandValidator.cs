using FluentValidation;

namespace Payroll.Application.Commands.Platform.Tenants;

internal sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    // Slug must be lowercase alphanumeric with hyphens, 3–63 chars total.
    // This regex is load-bearing: it prevents SQL injection in CREATE SCHEMA raw SQL.
    private const string SlugPattern = @"^[a-z0-9][a-z0-9\-]{1,61}[a-z0-9]$";

    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches(SlugPattern)
            .WithMessage("Slug must be 3–63 lowercase alphanumeric characters and hyphens, no leading or trailing hyphens.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
