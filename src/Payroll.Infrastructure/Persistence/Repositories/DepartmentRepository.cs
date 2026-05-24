using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(PayrollDbContext db) : IDepartmentRepository
{
    public async Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default) =>
        await db.Departments.OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Department department, CancellationToken ct = default) =>
        await db.Departments.AddAsync(department, ct);

    public void Update(Department department) =>
        db.Departments.Update(department);

    public void Remove(Department department) =>
        db.Departments.Remove(department);
}
