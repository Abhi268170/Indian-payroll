using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record UpdatePersonalDetailsCommand(
    Guid EmployeeId,
    string? DateOfBirth,
    string? FathersName,
    string? PAN,
    string? PersonalEmail,
    string DifferentlyAbledType,
    bool IsPWD,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? ResidentialState,
    string? PinCode,
    Guid ActorId) : IRequest;

internal sealed class UpdatePersonalDetailsValidator : AbstractValidator<UpdatePersonalDetailsCommand>
{
    public UpdatePersonalDetailsValidator()
    {
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .When(x => x.DateOfBirth is not null)
            .WithMessage("Date of birth cannot be empty if provided.");
        RuleFor(x => x.PAN).Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]$")
            .When(x => !string.IsNullOrEmpty(x.PAN)).WithMessage("PAN must be in format AAAAA9999A.");
        RuleFor(x => x.PersonalEmail).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.PersonalEmail)).WithMessage("Invalid email.");
        RuleFor(x => x.DifferentlyAbledType)
            .Must(d => Enum.TryParse<DifferentlyAbledType>(d, out _)).WithMessage("Invalid type.");
        RuleFor(x => x.ResidentialState)
            .Must(s => Enum.TryParse<IndianState>(s, out _))
            .When(x => !string.IsNullOrEmpty(x.ResidentialState)).WithMessage("Invalid state.");
        RuleFor(x => x.PinCode).Matches(@"^\d{6}$")
            .When(x => !string.IsNullOrEmpty(x.PinCode)).WithMessage("PIN code must be 6 digits.");
    }
}

public sealed class UpdatePersonalDetailsHandler(
    IEmployeeRepository repo,
    IEncryptionService enc,
    IUnitOfWork uow)
    : IRequestHandler<UpdatePersonalDetailsCommand>
{
    public async Task Handle(UpdatePersonalDetailsCommand req, CancellationToken ct)
    {
        Domain.Entities.Employee employee = await repo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        string? encryptedPan = req.PAN is not null ? enc.Encrypt(req.PAN) : null;
        IndianState? state = req.ResidentialState is not null
            ? Enum.Parse<IndianState>(req.ResidentialState)
            : null;

        DateOnly dob = req.DateOfBirth is not null
            ? DateOnly.Parse(req.DateOfBirth)
            : employee.DateOfBirth;

        employee.UpdatePersonalDetails(
            dob,
            req.FathersName,
            encryptedPan,
            req.PersonalEmail,
            Enum.Parse<DifferentlyAbledType>(req.DifferentlyAbledType),
            req.IsPWD,
            req.AddressLine1,
            req.AddressLine2,
            req.City,
            state,
            req.PinCode,
            req.ActorId);

        repo.Update(employee);
        await uow.SaveChangesAsync(ct);
    }
}
