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

    public Task<IReadOnlyList<PayrunEmployee>> GetByRunIdWithStatusAsync(Guid payrollRunId, PayrunEmployeeStatus status, CancellationToken ct = default) =>
        db.PayrunEmployees
            .Where(e => e.PayrollRunId == payrollRunId && e.Status == status)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<PayrunEmployee>>(t => t.Result, ct);
}
