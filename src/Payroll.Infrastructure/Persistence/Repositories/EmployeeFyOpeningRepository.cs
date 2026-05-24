using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeFyOpeningRepository(PayrollDbContext db) : IEmployeeFyOpeningRepository
{
    public Task<EmployeeFyOpening?> GetAsync(Guid employeeId, int fiscalYear, CancellationToken ct = default) =>
        db.EmployeeFyOpenings
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && e.FiscalYear == fiscalYear, ct);

    public async Task<IReadOnlyList<EmployeeFyOpening>> GetByEmployeesAndFiscalYearAsync(
        IEnumerable<Guid> employeeIds, int fiscalYear, CancellationToken ct = default)
    {
        var ids = employeeIds.ToList();
        return await db.EmployeeFyOpenings
            .Where(e => ids.Contains(e.EmployeeId) && e.FiscalYear == fiscalYear)
            .ToListAsync(ct);
    }

    public async Task AddAsync(EmployeeFyOpening entity, CancellationToken ct = default) =>
        await db.EmployeeFyOpenings.AddAsync(entity, ct);

    public void Update(EmployeeFyOpening entity) =>
        db.EmployeeFyOpenings.Update(entity);
}
