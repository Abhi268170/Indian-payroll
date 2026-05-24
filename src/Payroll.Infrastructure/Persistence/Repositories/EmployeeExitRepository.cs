using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeExitRepository(PayrollDbContext db) : IEmployeeExitRepository
{
    public Task<EmployeeExit?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.EmployeeExits.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<EmployeeExit?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        db.EmployeeExits.FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);

    public Task AddAsync(EmployeeExit exit, CancellationToken ct = default) =>
        db.EmployeeExits.AddAsync(exit, ct).AsTask();

    public void Update(EmployeeExit exit) => db.EmployeeExits.Update(exit);

    public void Remove(EmployeeExit exit) => db.EmployeeExits.Remove(exit);
}
