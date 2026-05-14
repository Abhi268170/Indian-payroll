using FluentValidation;

namespace Payroll.Application.Commands.OrgStructure;

internal sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    // PAN format: 5 alpha, 4 digit, 1 alpha (ABCDE1234F)
    private const string PanPattern = @"^[A-Z]{5}[0-9]{4}[A-Z]{1}$";

    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PAN).NotEmpty().Matches(PanPattern)
            .WithMessage("PAN must be in format ABCDE1234F");
        RuleFor(x => x.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("Employee must be at least 18 years old");
        RuleFor(x => x.DateOfJoining).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
    }
}
