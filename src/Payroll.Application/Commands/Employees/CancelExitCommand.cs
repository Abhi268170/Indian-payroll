using FluentValidation;
using MediatR;
using Payroll.Domain.Common;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.Employees;

public record CancelExitCommand(Guid EmployeeId, Guid ActorId) : IRequest;

public sealed class CancelExitCommandValidator : AbstractValidator<CancelExitCommand>
{
    public CancelExitCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
    }
}

public sealed class CancelExitHandler(
    IEmployeeRepository employeeRepo,
    IEmployeeExitRepository exitRepo,
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmpRepo,
    IPayrunComponentBreakdownRepository breakdownRepo,
    ITdsWorksheetRepository tdsWorksheetRepo,
    IUnitOfWork uow)
    : IRequestHandler<CancelExitCommand>
{
    public async Task Handle(CancelExitCommand req, CancellationToken ct)
    {
        Employee employee = await employeeRepo.GetByIdAsync(req.EmployeeId, ct)
            ?? throw new NotFoundException($"Employee {req.EmployeeId} not found.");

        EmployeeExit exit = await exitRepo.GetActiveByEmployeeAsync(req.EmployeeId, ct)
            ?? throw new DomainException("No exit is in progress for this employee.");

        // The strip in InitiateExitCommand removes the employee from any Draft
        // regular run covering the LWD month. Reversing that cleanly requires
        // re-seeding + recomputing the employee in that run — not yet supported.
        // Block rather than silently leave them missing from a run they belong in.
        var draftRegularRuns = await runRepo.FindDraftRegularRunsCoveringDateAsync(exit.LastWorkingDay, ct);
        if (draftRegularRuns.Count > 0)
        {
            int month = exit.LastWorkingDay.Month;
            int year = exit.LastWorkingDay.Year;
            throw new DomainException(
                $"This employee was removed from the Draft {year}-{month:D2} regular pay run when the exit "
                + "was initiated. Recompute that pay run to re-include them — automatic re-add on cancel "
                + "is not yet supported.");
        }

        // Clean up the FnF run this exit created/joined.
        if (exit.FnfPayrollRunId is Guid fnfRunId)
        {
            PayrollRun? fnfRun = await runRepo.GetByIdAsync(fnfRunId, ct);
            if (fnfRun != null)
            {
                if (fnfRun.Status != PayrollRunStatus.Draft)
                    throw new DomainException(
                        "Cannot cancel exit: the final settlement run has already been approved. "
                        + "Reject the approval first.");

                PayrunEmployee? pe = await payrunEmpRepo.GetByRunAndEmployeeAsync(fnfRunId, req.EmployeeId, ct);
                if (pe != null) payrunEmpRepo.Remove(pe);

                await breakdownRepo.RemoveRangeByRunAndEmployeeAsync(fnfRunId, req.EmployeeId, ct);
                await tdsWorksheetRepo.DeleteByRunAndEmployeeAsync(fnfRunId, req.EmployeeId, ct);

                if (fnfRun.Type == PayrollRunType.BulkFinalSettlement && fnfRun.EmployeeCount > 1)
                {
                    // Other exiting employees remain in this bulk run — keep it.
                    fnfRun.SetEmployeeCount(fnfRun.EmployeeCount - 1, req.ActorId);
                    runRepo.Update(fnfRun);
                }
                else
                {
                    // Single FinalSettlement, or the last employee in a bulk run.
                    fnfRun.Delete(req.ActorId);
                    runRepo.Update(fnfRun);
                }
            }
        }

        employee.RevertExit(req.ActorId);
        employeeRepo.Update(employee);

        exit.MarkReverted(req.ActorId);
        exitRepo.Update(exit);

        await uow.SaveChangesAsync(ct);
    }
}
