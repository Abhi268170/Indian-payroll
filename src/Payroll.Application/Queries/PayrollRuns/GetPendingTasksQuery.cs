using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Common;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.Entities;

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
        PayrollRun run = await runRepo.GetByIdAsync(req.RunId, ct)
            ?? throw new NotFoundException($"Payroll run {req.RunId} not found.");

        bool isFnf = run.Type == PayrollRunType.FinalSettlement
                  || run.Type == PayrollRunType.BulkFinalSettlement;

        var payrunEmployees = await payrunEmployeeRepo.GetByRunIdAsync(req.RunId, ct);
        IReadOnlyList<Domain.Entities.Employee> employees = await employeeRepo.GetManyByIdsAsync(
            payrunEmployees.Select(e => e.EmployeeId), ct);
        Dictionary<Guid, Domain.Entities.Employee> employeeMap = employees.ToDictionary(e => e.Id);

        var hardBlocks = new List<PendingTaskItemDto>();
        var softWarnings = new List<PendingTaskItemDto>();

        foreach (var pe in payrunEmployees)
        {
            // WI-10: FnF runs must have been computed before approval.
            // GrossPay = 0 means UpdateFnfRunCommand was never called — the operator
            // hasn't opened the settlement and saved it. Approving a ₹0 FnF run
            // produces a legally invalid payslip and marks the employee Exited.
            if (isFnf && pe.Status == PayrunEmployeeStatus.Active && pe.GrossPay == 0m)
            {
                employeeMap.TryGetValue(pe.EmployeeId, out Employee? fnfEmp);
                string name = fnfEmp?.EmployeeCode ?? pe.EmployeeId.ToString();
                hardBlocks.Add(new PendingTaskItemDto(pe.EmployeeId, name,
                    $"FnF settlement not computed for {name}. Open settlement and save it first."));
                continue;
            }

            // System-skipped = hard block (onboarding incomplete)
            if (pe.Status == PayrunEmployeeStatus.Skipped &&
                pe.SkipReason is not null &&
                pe.SkipReason.StartsWith("Onboarding incomplete", StringComparison.OrdinalIgnoreCase))
            {
                employeeMap.TryGetValue(pe.EmployeeId, out Domain.Entities.Employee? emp);
                hardBlocks.Add(new PendingTaskItemDto(pe.EmployeeId, emp?.EmployeeCode ?? pe.EmployeeId.ToString(), pe.SkipReason));
                continue;
            }

            // Active employee without PAN = soft warning (TDS at 20% §206AA)
            if (pe.Status == PayrunEmployeeStatus.Active)
            {
                if (employeeMap.TryGetValue(pe.EmployeeId, out Domain.Entities.Employee? emp) &&
                    string.IsNullOrWhiteSpace(emp.EncryptedPAN))
                {
                    softWarnings.Add(new PendingTaskItemDto(pe.EmployeeId, emp.EmployeeCode, "PAN not provided — TDS deducted at 20% (§206AA)"));
                }
            }
        }

        return new PendingTasksDto(hardBlocks, softWarnings);
    }
}
