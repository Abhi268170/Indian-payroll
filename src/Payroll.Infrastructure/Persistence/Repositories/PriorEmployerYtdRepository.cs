using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PriorEmployerYtdRepository(PayrollDbContext db) : IPriorEmployerYtdRepository
{
    public Task<IReadOnlyList<PriorEmployerYtd>> GetByEmployeesAndFiscalYearAsync(
        IReadOnlyList<Guid> employeeIds, int fiscalYear, CancellationToken ct = default) =>
        db.PriorEmployerYtds
            .Where(p => employeeIds.Contains(p.EmployeeId) && p.FinancialYear == fiscalYear && !p.IsDeleted)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PriorEmployerYtd>>(t => t.Result, ct);
}
