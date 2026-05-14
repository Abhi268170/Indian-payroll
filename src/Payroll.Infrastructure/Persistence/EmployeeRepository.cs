using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class EmployeeRepository(PayrollDbContext db) : IEmployeeRepository
{
    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == code, cancellationToken);

    public async Task<IReadOnlyList<Employee>> GetActiveByTenantAsync(CancellationToken cancellationToken = default) =>
        await db.Employees.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await db.Employees.AddAsync(employee, cancellationToken);
}
