using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record CreateEmployeeCommand(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? EmployeeCode,
    string WorkEmail,
    string? MobileNumber,
    string Gender,
    string DateOfJoining,
    string DateOfBirth,
    string EmploymentType,
    bool IsDirector,
    bool EnablePortalAccess,
    Guid DepartmentId,
    Guid DesignationId,
    Guid WorkLocationId,
    Guid? BusinessUnitId,
    Guid? CostCentreId,
    Guid ActorId) : IRequest<Guid>;

internal sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100).When(x => x.MiddleName != null);
        RuleFor(x => x.WorkEmail).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.MobileNumber)
            .Matches(@"^\d{10}$")
            .When(x => !string.IsNullOrEmpty(x.MobileNumber))
            .WithMessage("Mobile number must be 10 digits.");
        RuleFor(x => x.Gender).NotEmpty()
            .Must(g => Enum.TryParse<Gender>(g, out _)).WithMessage("Invalid gender.");
        RuleFor(x => x.DateOfJoining).NotEmpty();
        RuleFor(x => x.DateOfBirth).NotEmpty();
        RuleFor(x => x.EmploymentType).NotEmpty()
            .Must(e => Enum.TryParse<EmploymentType>(e, out _)).WithMessage("Invalid employment type.");
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.WorkLocationId).NotEmpty();
    }
}

public sealed class CreateEmployeeHandler(
    IEmployeeRepository repo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEmployeeCommand req, CancellationToken ct)
    {
        if (await repo.EmailExistsAsync(req.WorkEmail, null, ct))
            throw new DomainException("Work email already in use.");

        string code = !string.IsNullOrWhiteSpace(req.EmployeeCode)
            ? req.EmployeeCode.Trim().ToUpperInvariant()
            : await repo.NextEmployeeCodeAsync(ct);

        if (await repo.CodeExistsAsync(code, null, ct))
            throw new DomainException($"Employee code '{code}' already in use.");

        Employee employee = Employee.CreateStep1(
            req.FirstName,
            req.MiddleName,
            req.LastName,
            code,
            req.WorkEmail,
            req.MobileNumber,
            Enum.Parse<Gender>(req.Gender),
            DateOnly.Parse(req.DateOfJoining),
            Enum.Parse<EmploymentType>(req.EmploymentType),
            req.IsDirector,
            req.EnablePortalAccess,
            tenantContext.TenantId,
            req.DepartmentId,
            req.DesignationId,
            req.WorkLocationId,
            req.BusinessUnitId,
            req.CostCentreId,
            DateOnly.Parse(req.DateOfBirth),
            req.ActorId);

        await repo.AddAsync(employee, ct);
        await uow.SaveChangesAsync(ct);
        return employee.Id;
    }
}
