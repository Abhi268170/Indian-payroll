using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(PayrollDbContext db) : IEmployeeRepository
{
    public async Task<IReadOnlyList<Employee>> ListAsync(CancellationToken ct = default) =>
        await db.Employees.OrderBy(e => e.EmployeeCode).ToListAsync(ct);

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Employee?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == code, ct);

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct = default) =>
        db.Employees.AnyAsync(e => e.WorkEmail == email && (excludeId == null || e.Id != excludeId), ct);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct = default) =>
        db.Employees.AnyAsync(e => e.EmployeeCode == code && (excludeId == null || e.Id != excludeId), ct);

    public async Task<string> NextEmployeeCodeAsync(CancellationToken ct = default)
    {
        int count = await db.Employees.CountAsync(ct);
        return $"EMP{(count + 1):D3}";
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
        await db.Employees.AddAsync(employee, ct);

    public void Update(Employee employee) =>
        db.Employees.Update(employee);
}
