using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;
using Payroll.Domain.ValueObjects;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayrollRunRepository(PayrollDbContext db) : IPayrollRunRepository
{
    public Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<PayrollRun?> GetByPeriodAsync(PayPeriod period, CancellationToken ct = default) =>
        db.PayrollRuns.FirstOrDefaultAsync(r => r.PayPeriod == period, ct);

    public Task<PayrollRun?> GetLatestPaidAsync(CancellationToken ct = default) =>
        db.PayrollRuns
            .Where(r => r.Status == Domain.Enums.PayrollRunStatus.Paid)
            .OrderByDescending(r => r.PayPeriod.Year)
            .ThenByDescending(r => r.PayPeriod.Month)
            .FirstOrDefaultAsync(ct);

    public Task<IReadOnlyList<PayrollRun>> GetHistoryAsync(int skip, int take, CancellationToken ct = default) =>
        db.PayrollRuns
            .OrderByDescending(r => r.PayPeriod.Year)
            .ThenByDescending(r => r.PayPeriod.Month)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrollRun>>(t => t.Result, ct);

    public async Task AddAsync(PayrollRun run, CancellationToken ct = default) =>
        await db.PayrollRuns.AddAsync(run, ct);

    public void Update(PayrollRun run) =>
        db.PayrollRuns.Update(run);

    public Task<bool> ExistsForPeriodAsync(PayPeriod period, CancellationToken ct = default) =>
        db.PayrollRuns.AnyAsync(r => r.PayPeriod == period, ct);

    public Task<PayrollRun?> GetActiveForPeriodAsync(PayPeriod period, CancellationToken ct = default) =>
        db.PayrollRuns.FirstOrDefaultAsync(
            r => r.PayPeriod == period &&
                 r.Status != Domain.Enums.PayrollRunStatus.Deleted,
            ct);
}
