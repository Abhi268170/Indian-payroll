using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class WorkLocationRepository(PayrollDbContext db) : IWorkLocationRepository
{
    public async Task<IReadOnlyList<WorkLocation>> ListAsync(CancellationToken ct = default) =>
        await db.WorkLocations.OrderBy(w => w.Name).ToListAsync(ct);

    public Task<WorkLocation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.WorkLocations.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddAsync(WorkLocation workLocation, CancellationToken ct = default) =>
        await db.WorkLocations.AddAsync(workLocation, ct);

    public void Update(WorkLocation workLocation) =>
        db.WorkLocations.Update(workLocation);

    public void Remove(WorkLocation workLocation) =>
        db.WorkLocations.Remove(workLocation);

    public Task<int> GetEmployeeCountAsync(Guid id, CancellationToken ct = default)
    {
        // TODO: query employee count when Employee entity exists
        return Task.FromResult(0);
    }
}
