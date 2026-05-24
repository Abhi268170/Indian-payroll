using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record UpsertEmployeeFyOpeningCommand(
    Guid EmployeeId,
    int FiscalYear,
    int MonthsCount,
    decimal GrossSalary,
    decimal TdsDeducted,
    decimal PfDeducted,
    Guid ActorId) : IRequest;

public sealed class UpsertEmployeeFyOpeningValidator : AbstractValidator<UpsertEmployeeFyOpeningCommand>
{
    public UpsertEmployeeFyOpeningValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.FiscalYear).InclusiveBetween(2020, 2100);
        RuleFor(x => x.MonthsCount).InclusiveBetween(1, 12);
        RuleFor(x => x.GrossSalary).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TdsDeducted).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PfDeducted).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class UpsertEmployeeFyOpeningHandler(
    IEmployeeFyOpeningRepository repo,
    IUnitOfWork uow)
    : IRequestHandler<UpsertEmployeeFyOpeningCommand>
{
    public async Task Handle(UpsertEmployeeFyOpeningCommand req, CancellationToken ct)
    {
        EmployeeFyOpening? existing = await repo.GetAsync(req.EmployeeId, req.FiscalYear, ct);
        if (existing is null)
        {
            await repo.AddAsync(EmployeeFyOpening.Create(
                req.EmployeeId, req.FiscalYear, req.MonthsCount,
                req.GrossSalary, req.TdsDeducted, req.PfDeducted, req.ActorId), ct);
        }
        else
        {
            existing.Update(req.MonthsCount, req.GrossSalary, req.TdsDeducted, req.PfDeducted, req.ActorId);
            repo.Update(existing);
        }
        await uow.SaveChangesAsync(ct);
    }
}
