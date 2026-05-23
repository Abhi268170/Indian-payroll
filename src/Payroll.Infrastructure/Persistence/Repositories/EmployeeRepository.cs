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

    public async Task<HashSet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        List<string> list = codes.Select(c => c.ToUpperInvariant()).ToList();
        List<string> found = await db.Employees
            .Where(e => list.Contains(e.EmployeeCode))
            .Select(e => e.EmployeeCode)
            .ToListAsync(ct);
        return found.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> GetExistingEmailsAsync(IEnumerable<string> emails, CancellationToken ct = default)
    {
        List<string> list = emails.Select(e => e.ToLowerInvariant()).Distinct().ToList();
        List<string> found = await db.Employees
            .Where(e => list.Contains(e.WorkEmail.ToLower()))
            .Select(e => e.WorkEmail)
            .ToListAsync(ct);
        return found.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<Employee>> GetManyByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        List<Guid> list = ids.Distinct().ToList();
        return await db.Employees.Where(e => list.Contains(e.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetManyByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        List<string> list = codes.Select(c => c.ToUpperInvariant()).ToList();
        return await db.Employees.Where(e => list.Contains(e.EmployeeCode)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Employee>> GetManyByEmailsAsync(IEnumerable<string> emails, CancellationToken ct = default)
    {
        List<string> list = emails.Select(e => e.ToLowerInvariant()).Distinct().ToList();
        return await db.Employees.Where(e => list.Contains(e.WorkEmail.ToLower())).ToListAsync(ct);
    }
}
