using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class PayrunEmployeeRepository(PayrollDbContext db) : IPayrunEmployeeRepository
{
    public Task<IReadOnlyList<PayrunEmployee>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default) =>
        db.PayrunEmployees
            .Where(e => e.PayrollRunId == payrollRunId)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunEmployee>>(t => t.Result, ct);

    public Task<PayrunEmployee?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default) =>
        db.PayrunEmployees.FirstOrDefaultAsync(e => e.PayrollRunId == payrollRunId && e.EmployeeId == employeeId, ct);

    public async Task AddRangeAsync(IEnumerable<PayrunEmployee> employees, CancellationToken ct = default) =>
        await db.PayrunEmployees.AddRangeAsync(employees, ct);

    public async Task AddAsync(PayrunEmployee employee, CancellationToken ct = default) =>
        await db.PayrunEmployees.AddAsync(employee, ct);

    public void Update(PayrunEmployee employee) =>
        db.PayrunEmployees.Update(employee);

    public void Remove(PayrunEmployee employee) =>
        db.PayrunEmployees.Remove(employee);

    public Task<IReadOnlyList<PayrunEmployee>> GetByRunIdWithStatusAsync(Guid payrollRunId, PayrunEmployeeStatus status, CancellationToken ct = default) =>
        db.PayrunEmployees
            .Where(e => e.PayrollRunId == payrollRunId && e.Status == status)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunEmployee>>(t => t.Result, ct);

    public Task<IReadOnlyList<PayrunEmployee>> GetByEmployeeAndRunIdsAsync(Guid employeeId, IEnumerable<Guid> runIds, CancellationToken ct = default)
    {
        var ids = runIds.ToList();
        return db.PayrunEmployees
            .Where(e => e.EmployeeId == employeeId && ids.Contains(e.PayrollRunId))
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunEmployee>>(t => t.Result, ct);
    }

    public async Task<Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)>> GetCurrentEmployerYtdAsync(
        IEnumerable<Guid> employeeIds, int fiscalYear, CancellationToken ct = default)
    {
        var empIds = employeeIds.ToList();
        var rows = await (
            from pe in db.PayrunEmployees
            join run in db.PayrollRuns on pe.PayrollRunId equals run.Id
            where empIds.Contains(pe.EmployeeId)
                && (run.Status == PayrollRunStatus.Approved || run.Status == PayrollRunStatus.Paid)
                && ((run.PayPeriod.Year == fiscalYear && run.PayPeriod.Month >= 4)
                    || (run.PayPeriod.Year == fiscalYear + 1 && run.PayPeriod.Month <= 3))
            group pe by pe.EmployeeId into g
            select new
            {
                EmployeeId = g.Key,
                YtdGross = g.Sum(x => x.GrossPay),
                YtdTaxableGross = g.Sum(x => x.TaxableGrossPay),
                YtdTds = g.Sum(x => x.TdsAmount),
            }
        ).ToListAsync(ct);

        return rows.ToDictionary(r => r.EmployeeId, r => (r.YtdGross, r.YtdTaxableGross, r.YtdTds));
    }
}
