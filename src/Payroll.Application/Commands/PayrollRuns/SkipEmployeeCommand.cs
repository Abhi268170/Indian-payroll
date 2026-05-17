using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record SkipEmployeeCommand(Guid RunId, Guid EmployeeId, string Reason, Guid ActorId) : IRequest;

public sealed class SkipEmployeeCommandValidator : AbstractValidator<SkipEmployeeCommand>
{
    public SkipEmployeeCommandValidator()
    {
        RuleFor(x => x.RunId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Skip reason is required.");
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class SkipEmployeeHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IUnitOfWork uow)
    : IRequestHandler<SkipEmployeeCommand>
{
    public async Task Handle(SkipEmployeeCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Draft)
            throw new InvalidOperationException("Employees can only be skipped on a Draft payroll run.");

        var payrunEmp = await payrunEmployeeRepo.GetByRunAndEmployeeAsync(req.RunId, req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not in this payroll run.");

        payrunEmp.Skip(req.Reason, req.ActorId);
        payrunEmployeeRepo.Update(payrunEmp);

        await RecalculateRunTotals(run, req.RunId, req.ActorId, payrunEmployeeRepo);

        runRepo.Update(run);
        await uow.SaveChangesAsync(ct);
    }

    internal static async Task RecalculateRunTotals(
        Domain.Entities.PayrollRun run,
        Guid runId,
        Guid actorId,
        IPayrunEmployeeRepository payrunEmployeeRepo)
    {
        var allEmployees = await payrunEmployeeRepo.GetByRunIdAsync(runId);
        var active = allEmployees.Where(e => e.Status != PayrunEmployeeStatus.Skipped).ToList();

        decimal totalNetPay = active.Sum(e => e.NetPay);
        decimal totalEmployerPf = active.Sum(e => e.EmployerPf);
        decimal totalEmployerEsi = active.Sum(e => e.EmployerEsi);
        decimal totalEdli = active.Sum(e => e.EdliAmount);
        decimal totalTds = active.Sum(e => e.TdsAmount);
        decimal totalPt = active.Sum(e => e.PtAmount);
        decimal payrollCost = totalNetPay + totalEmployerPf + totalEmployerEsi + totalEdli;

        run.UpdateFinancialSummary(
            payrollCost, totalNetPay, totalEmployerPf, totalEmployerEsi, totalEdli,
            totalTds, totalPt, allEmployees.Count, actorId);
    }
}
