using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetPendingPayrollRunsQuery : IRequest<IReadOnlyList<PendingRunCardDto>>;

internal sealed class GetPendingPayrollRunsHandler(
    IPayrollRunRepository runRepo,
    IEmployeeExitRepository exitRepo,
    IEmployeeRepository employeeRepo)
    : IRequestHandler<GetPendingPayrollRunsQuery, IReadOnlyList<PendingRunCardDto>>
{
    public async Task<IReadOnlyList<PendingRunCardDto>> Handle(GetPendingPayrollRunsQuery _, CancellationToken ct)
    {
        var runs = await runRepo.ListPendingAsync(ct);
        if (runs.Count == 0) return Array.Empty<PendingRunCardDto>();

        // Resolve Single FinalSettlement primary employee labels.
        var fsRunIds = runs.Where(r => r.Type == PayrollRunType.FinalSettlement).Select(r => r.Id).ToList();
        var exits = await exitRepo.GetByFnfRunIdsAsync(fsRunIds, ct);
        var exitByRunId = exits
            .Where(e => e.FnfPayrollRunId.HasValue)
            .GroupBy(e => e.FnfPayrollRunId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var employeeIds = exitByRunId.Values.Select(e => e.EmployeeId).Distinct().ToList();
        var employees = employeeIds.Count == 0
            ? Array.Empty<Domain.Entities.Employee>()
            : await employeeRepo.GetManyByIdsAsync(employeeIds, ct);
        var employeeById = employees.ToDictionary(e => e.Id);

        return runs.Select(r =>
        {
            string? label = null;
            if (r.Type == PayrollRunType.FinalSettlement
                && exitByRunId.TryGetValue(r.Id, out var exit)
                && employeeById.TryGetValue(exit.EmployeeId, out var emp))
            {
                label = $"{emp.FullName} ({emp.EmployeeCode})";
            }

            return new PendingRunCardDto(
                Id: r.Id,
                Type: r.Type.ToString(),
                Status: r.Status.ToString(),
                Year: r.PayPeriod.Year,
                Month: r.PayPeriod.Month,
                PeriodLabel: new DateTime(r.PayPeriod.Year, r.PayPeriod.Month, 1).ToString("MMMM yyyy"),
                PayDay: r.PayDay,
                TotalNetPay: r.TotalNetPay,
                EmployeeCount: r.EmployeeCount,
                PrimaryEmployeeLabel: label);
        }).ToList();
    }
}
