using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record UpdateBasicDetailsCommand(
    Guid EmployeeId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? MobileNumber,
    string Gender,
    bool IsDirector,
    bool EnablePortalAccess,
    Guid DepartmentId,
    Guid DesignationId,
    Guid WorkLocationId,
    Guid? BusinessUnitId,
    Guid? CostCentreId,
    Guid ActorId) : IRequest;

internal sealed class UpdateBasicDetailsValidator : AbstractValidator<UpdateBasicDetailsCommand>
{
    public UpdateBasicDetailsValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100).When(x => x.MiddleName != null);
        RuleFor(x => x.MobileNumber).Matches(@"^\d{10}$")
            .When(x => !string.IsNullOrEmpty(x.MobileNumber)).WithMessage("Mobile number must be 10 digits.");
        RuleFor(x => x.Gender).NotEmpty()
            .Must(g => Enum.TryParse<Gender>(g, out _)).WithMessage("Invalid gender.");
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.DesignationId).NotEmpty();
        RuleFor(x => x.WorkLocationId).NotEmpty();
    }
}

public sealed class UpdateBasicDetailsHandler(IEmployeeRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateBasicDetailsCommand>
{
    public async Task Handle(UpdateBasicDetailsCommand req, CancellationToken ct)
    {
        Domain.Entities.Employee employee = await repo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        employee.UpdateBasicDetails(
            req.FirstName, req.MiddleName, req.LastName,
            req.MobileNumber,
            Enum.Parse<Gender>(req.Gender),
            req.IsDirector, req.EnablePortalAccess,
            req.DepartmentId, req.DesignationId, req.WorkLocationId,
            req.BusinessUnitId, req.CostCentreId,
            req.ActorId);

        repo.Update(employee);
        await uow.SaveChangesAsync(ct);
    }
}
