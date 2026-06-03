using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class SalaryRevisionRepository(PayrollDbContext db)
    : ISalaryRevisionRepository
{
    public async Task AddAsync(SalaryRevision revision, CancellationToken ct = default) =>
        await db.SalaryRevisions.AddAsync(revision, ct);

    public Task<SalaryRevision?> GetByIdWithOverridesAsync(Guid id, CancellationToken ct = default) =>
        db.SalaryRevisions
            .Include(r => r.ComponentOverrides)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<SalaryRevision>> GetByEmployeeAsync(
        Guid employeeId, CancellationToken ct = default) =>
        await db.SalaryRevisions
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.PayoutYear)
            .ThenByDescending(r => r.PayoutMonth)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SalaryRevision>> GetAppliedUnpaidForPayoutAsync(
        int payoutYear, int payoutMonth, CancellationToken ct = default) =>
        await db.SalaryRevisions
            .Include(r => r.ComponentOverrides)
            .Where(r => r.PayoutYear == payoutYear
                && r.PayoutMonth == payoutMonth
                && r.Status == SalaryRevisionStatus.Applied
                && r.ArrearPaidRunId == null)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SalaryRevision>> GetByArrearPaidRunAsync(
        Guid runId, CancellationToken ct = default) =>
        await db.SalaryRevisions
            .Where(r => r.ArrearPaidRunId == runId)
            .ToListAsync(ct);

    public void Update(SalaryRevision revision) =>
        db.SalaryRevisions.Update(revision);
}
