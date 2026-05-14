using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence;

internal sealed class DepartmentRepository(PayrollDbContext db) : IDepartmentRepository
{
    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Departments.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default) =>
        await db.Departments.AddAsync(department, cancellationToken);
}
