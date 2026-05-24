using MediatR;
using Payroll.Domain.Interfaces;

namespace Payroll.Application.Queries.Payslips;

public record EmployeePayslipDto(
    Guid Id,
    int PayPeriodYear,
    int PayPeriodMonth,
    DateTimeOffset GeneratedAt,
    string PdfStorageKey,
    bool IsPublished,
    decimal NetPay);

public record GetEmployeePayslipsQuery(Guid EmployeeId) : IRequest<IReadOnlyList<EmployeePayslipDto>>;

public sealed class GetEmployeePayslipsHandler(
    IPayslipRepository payslipRepo,
    IPayrollRunRepository payrollRunRepo)
    : IRequestHandler<GetEmployeePayslipsQuery, IReadOnlyList<EmployeePayslipDto>>
{
    public async Task<IReadOnlyList<EmployeePayslipDto>> Handle(
        GetEmployeePayslipsQuery req, CancellationToken ct)
    {
        IReadOnlyList<Domain.Entities.Payslip> payslips =
            await payslipRepo.GetByEmployeeIdAsync(req.EmployeeId, ct);

        if (payslips.Count == 0)
            return [];

        // Batch-load payroll runs to get pay period info (avoids N+1)
        IEnumerable<Guid> runIds = payslips.Select(p => p.PayrollRunId).Distinct();
        Dictionary<Guid, Domain.Entities.PayrollRun> runs = new();
        foreach (Guid runId in runIds)
        {
            Domain.Entities.PayrollRun? run = await payrollRunRepo.GetByIdAsync(runId, ct);
            if (run is not null)
                runs[runId] = run;
        }

        return payslips
            .Where(p => runs.ContainsKey(p.PayrollRunId))
            .OrderByDescending(p => runs[p.PayrollRunId].PayPeriod.Year)
            .ThenByDescending(p => runs[p.PayrollRunId].PayPeriod.Month)
            .Select(p => new EmployeePayslipDto(
                p.Id,
                runs[p.PayrollRunId].PayPeriod.Year,
                runs[p.PayrollRunId].PayPeriod.Month,
                p.GeneratedAt,
                p.PdfStorageKey,
                p.IsPublished,
                p.NetPay))
            .ToList();
    }
}
