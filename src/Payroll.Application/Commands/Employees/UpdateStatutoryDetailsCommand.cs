using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record UpdateStatutoryDetailsCommand(
    Guid EmployeeId,
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled,
    string? UAN,
    string? ESICIPNumber,
    Guid ActorId) : IRequest;

internal sealed class UpdateStatutoryDetailsValidator : AbstractValidator<UpdateStatutoryDetailsCommand>
{
    public UpdateStatutoryDetailsValidator()
    {
        RuleFor(x => x.UAN).Matches(@"^\d{12}$")
            .When(x => !string.IsNullOrEmpty(x.UAN)).WithMessage("UAN must be 12 digits.");
        RuleFor(x => x.ESICIPNumber).MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.ESICIPNumber));
    }
}

public sealed class UpdateStatutoryDetailsHandler(IEmployeeRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateStatutoryDetailsCommand>
{
    public async Task Handle(UpdateStatutoryDetailsCommand req, CancellationToken ct)
    {
        Domain.Entities.Employee employee = await repo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        employee.UpdateStatutoryDetails(
            req.EpfEnabled, req.EsiEnabled, req.PtEnabled, req.LwfEnabled,
            req.UAN, req.ESICIPNumber,
            req.ActorId);

        repo.Update(employee);
        await uow.SaveChangesAsync(ct);
    }
}
