using MediatR;
using Payroll.Application.Interfaces;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Commands.PayrollRuns;

public record RegeneratePayslipsCommand(Guid RunId) : IRequest;

public sealed class RegeneratePayslipsHandler(
    IPayrollRunRepository runRepo,
    IPayrollJobDispatcher jobDispatcher)
    : IRequestHandler<RegeneratePayslipsCommand>
{
    public async Task Handle(RegeneratePayslipsCommand req, CancellationToken ct)
    {
        var run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        if (run.Status != PayrollRunStatus.Paid)
            throw new InvalidOperationException("Payslips can only be regenerated for Paid runs.");

        jobDispatcher.EnqueueGeneratePayslips(run.Id, run.TenantId);
    }
}
