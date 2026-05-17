using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPendingTasksQuery(Guid RunId) : IRequest<PendingTasksDto>;

public sealed class GetPendingTasksHandler(
    IPayrollRunRepository runRepo,
    IPayrunEmployeeRepository payrunEmployeeRepo,
    IEmployeeRepository employeeRepo)
    : IRequestHandler<GetPendingTasksQuery, PendingTasksDto>
{
    public async Task<PendingTasksDto> Handle(GetPendingTasksQuery req, CancellationToken ct)
    {
        _ = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        var payrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);

        var hardBlocks = new List<PendingTaskItemDto>();
        var softWarnings = new List<PendingTaskItemDto>();

        foreach (var pe in payrunEmployees)
        {
            // System-skipped = hard block (onboarding incomplete)
            if (pe.Status == PayrunEmployeeStatus.Skipped &&
                pe.SkipReason is not null &&
                pe.SkipReason.StartsWith("Onboarding incomplete", StringComparison.OrdinalIgnoreCase))
            {
                var emp = await employeeRepo.GetByIdAsync(pe.EmployeeId, ct);
                hardBlocks.Add(new PendingTaskItemDto(pe.EmployeeId, emp?.EmployeeCode ?? pe.EmployeeId.ToString(), pe.SkipReason));
                continue;
            }

            // Active employee without PAN = soft warning (TDS at 20% §206AA)
            if (pe.Status == PayrunEmployeeStatus.Active)
            {
                var emp = await employeeRepo.GetByIdAsync(pe.EmployeeId, ct);
                if (emp is not null && string.IsNullOrWhiteSpace(emp.EncryptedPAN))
                    softWarnings.Add(new PendingTaskItemDto(pe.EmployeeId, emp.EmployeeCode, "PAN not provided — TDS deducted at 20% (§206AA)"));
            }
        }

        return new PendingTasksDto(hardBlocks, softWarnings);
    }
}
