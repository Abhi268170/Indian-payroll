using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class TdsWorksheetRepository(PayrollDbContext db) : ITdsWorksheetRepository
{
    public Task<TdsWorksheet?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default) =>
        db.TdsWorksheets.FirstOrDefaultAsync(w => w.PayrollRunId == payrollRunId && w.EmployeeId == employeeId, ct);

    public Task<IReadOnlyList<TdsWorksheet>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        db.TdsWorksheets
            .Where(w => w.PayrollRunId == payrollRunId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<TdsWorksheet>>(t => t.Result, ct);

    public async Task AddAsync(TdsWorksheet worksheet, CancellationToken ct = default) =>
        await db.TdsWorksheets.AddAsync(worksheet, ct);

    public async Task AddRangeAsync(IEnumerable<TdsWorksheet> worksheets, CancellationToken ct = default) =>
        await db.TdsWorksheets.AddRangeAsync(worksheets, ct);

    public async Task DeleteByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        await db.TdsWorksheets.Where(w => w.PayrollRunId == payrollRunId).ExecuteDeleteAsync(ct);
}
