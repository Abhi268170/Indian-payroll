using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record AssignSalaryStructureCommand(
    Guid EmployeeId,
    decimal AnnualCTC,
    Guid? SalaryStructureTemplateId,
    bool EpfEnabled,
    bool EsiEnabled,
    bool PtEnabled,
    bool LwfEnabled,
    Guid ActorId) : IRequest;

internal sealed class AssignSalaryStructureCommandValidator : AbstractValidator<AssignSalaryStructureCommand>
{
    public AssignSalaryStructureCommandValidator()
    {
        RuleFor(x => x.AnnualCTC).GreaterThan(0).WithMessage("Annual CTC must be greater than zero.");
    }
}

public sealed class AssignSalaryStructureHandler(
    IEmployeeRepository employeeRepo,
    IEmployeeSalaryStructureRepository salaryRepo,
    ITenantContext tenantContext,
    IUnitOfWork uow)
    : IRequestHandler<AssignSalaryStructureCommand>
{
    public async Task Handle(AssignSalaryStructureCommand req, CancellationToken ct)
    {
        Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        EmployeeSalaryStructure? existing = await salaryRepo.GetActiveAsync(req.EmployeeId, ct);
        if (existing is not null)
        {
            existing.Close(DateOnly.FromDateTime(DateTime.UtcNow), req.ActorId);
            salaryRepo.Update(existing);
        }

        EmployeeSalaryStructure structure = EmployeeSalaryStructure.Create(
            req.EmployeeId,
            tenantContext.TenantId,
            req.SalaryStructureTemplateId,
            req.AnnualCTC,
            DateOnly.FromDateTime(DateTime.UtcNow),
            req.ActorId);

        await salaryRepo.AddAsync(structure, ct);

        employee.UpdateStatutoryDetails(
            req.EpfEnabled, req.EsiEnabled, req.PtEnabled, req.LwfEnabled,
            employee.UAN, employee.ESICIPNumber, req.ActorId);

        employeeRepo.Update(employee);
        await uow.SaveChangesAsync(ct);
    }
}
