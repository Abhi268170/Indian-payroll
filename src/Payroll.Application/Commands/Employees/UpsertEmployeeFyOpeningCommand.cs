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
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IUnitOfWork uow)
    : IRequestHandler<UpsertEmployeeFyOpeningCommand>
{
    public async Task Handle(UpsertEmployeeFyOpeningCommand req, CancellationToken ct)
    {
        // Openings merge into YTD on every recompute. Editing one after runs are
        // approved in that FY retroactively shifts the employee's TDS basis and
        // breaks reproducibility of past computations.
        Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)> ytd =
            await payrunEmployeeRepo.GetCurrentEmployerYtdAsync([req.EmployeeId], req.FiscalYear, ct);
        if (ytd.TryGetValue(req.EmployeeId, out (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds) existing0)
            && (existing0.YtdGross != 0m || existing0.YtdTds != 0m))
            throw new DomainException(
                "FY opening balances cannot be changed once the employee has approved payroll runs in that fiscal year. " +
                "Reject the approvals first, or use a TDS override for corrections.");

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
