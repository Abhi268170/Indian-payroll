using MediatR;
using Payroll.Application.DTOs;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;
using Payroll.Engine;

namespace Payroll.Application.Queries.PayrollRuns;

public record GetCurrentPayPeriodQuery : IRequest<CurrentPayPeriodDto?>;

internal sealed class GetCurrentPayPeriodHandler(
    IPayScheduleRepository payScheduleRepo,
    IPayrollRunRepository payrollRunRepo,
    IEmployeeRepository employeeRepo)
    : IRequestHandler<GetCurrentPayPeriodQuery, CurrentPayPeriodDto?>
{
    public async Task<CurrentPayPeriodDto?> Handle(GetCurrentPayPeriodQuery _, CancellationToken ct)
    {
        var paySchedule = await payScheduleRepo.GetAsync(ct);
        if (paySchedule is null) return null;

        // Determine next payable period
        PayPeriod period;
        var latestPaid = await payrollRunRepo.GetLatestPaidAsync(PayrollRunType.Regular, ct);
        if (latestPaid is not null)
        {
            var next = latestPaid.PayPeriod.StartDate.AddMonths(1);
            period = new PayPeriod(next.Year, next.Month);
        }
        else if (paySchedule.FirstPayPeriodMonth.HasValue && paySchedule.FirstPayPeriodYear.HasValue)
        {
            period = new PayPeriod(paySchedule.FirstPayPeriodYear.Value, paySchedule.FirstPayPeriodMonth.Value);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            period = new PayPeriod(now.Year, now.Month);
        }

        // Resolve pay day
        EngineWorkWeekDay workWeek = (EngineWorkWeekDay)(int)paySchedule.WorkWeekDays;
        EnginePayDateType payDateType = paySchedule.PayDateType == PayDateType.LastDay
            ? EnginePayDateType.LastDay
            : EnginePayDateType.SpecificDay;
        DateOnly payDay = PayScheduleHelpers.ResolveActualPayDate(payDateType, paySchedule.PayDateDay,
            period.Year, period.Month, workWeek);

        // Check for outstanding run
        var activeRun = await payrollRunRepo.GetActiveForPeriodAsync(period, PayrollRunType.Regular, ct);
        var employees = await employeeRepo.ListAsync(ct);
        DateOnly periodEnd = period.EndDate;
        int activeCount = employees.Count(e =>
            e.Status == EmployeeStatus.Active
            && (e.DateOfLeaving == null || e.DateOfLeaving > periodEnd));

        return new CurrentPayPeriodDto(
            Year: period.Year,
            Month: period.Month,
            PeriodLabel: period.ToString(),
            PayDay: payDay,
            ActiveEmployeeCount: activeCount,
            HasOutstandingRun: activeRun is not null,
            OutstandingRunId: activeRun?.Id,
            OutstandingRunStatus: activeRun?.Status.ToString());
    }
}
