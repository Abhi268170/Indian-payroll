using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayrunComponentBreakdownRepository(PayrollDbContext db) : IPayrunComponentBreakdownRepository
{
    public Task<IReadOnlyList<PayrunComponentBreakdown>> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default) =>
        db.PayrunComponentBreakdowns
            .Where(b => b.PayrollRunId == payrollRunId && b.EmployeeId == employeeId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunComponentBreakdown>>(t => t.Result, ct);

    public Task<IReadOnlyList<PayrunComponentBreakdown>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        db.PayrunComponentBreakdowns
            .Where(b => b.PayrollRunId == payrollRunId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunComponentBreakdown>>(t => t.Result, ct);

    public async Task AddRangeAsync(IEnumerable<PayrunComponentBreakdown> breakdowns, CancellationToken ct = default) =>
        await db.PayrunComponentBreakdowns.AddRangeAsync(breakdowns, ct);

    public async Task AddAsync(PayrunComponentBreakdown breakdown, CancellationToken ct = default) =>
        await db.PayrunComponentBreakdowns.AddAsync(breakdown, ct);

    public void Update(PayrunComponentBreakdown breakdown) =>
        db.PayrunComponentBreakdowns.Update(breakdown);

    public void Remove(PayrunComponentBreakdown breakdown) =>
        db.PayrunComponentBreakdowns.Remove(breakdown);

    public async Task RemoveRangeByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default)
    {
        var rows = await db.PayrunComponentBreakdowns
            .Where(b => b.PayrollRunId == payrollRunId && b.EmployeeId == employeeId)
            .ToListAsync(ct);
        db.PayrunComponentBreakdowns.RemoveRange(rows);
    }
}
