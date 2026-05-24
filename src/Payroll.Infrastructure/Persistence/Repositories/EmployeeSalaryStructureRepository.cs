using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeSalaryStructureRepository(PayrollDbContext db)
    : IEmployeeSalaryStructureRepository
{
    public Task<EmployeeSalaryStructure?> GetActiveAsync(Guid employeeId, CancellationToken ct = default) =>
        db.EmployeeSalaryStructures
            .Where(s => s.EmployeeId == employeeId && s.EffectiveTo == null)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public Task<EmployeeSalaryStructure?> GetActiveWithOverridesAsync(Guid employeeId, CancellationToken ct = default) =>
        db.EmployeeSalaryStructures
            .Include(s => s.ComponentOverrides)
            .Where(s => s.EmployeeId == employeeId && s.EffectiveTo == null)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public async Task<HashSet<Guid>> GetEmployeesWithActiveStructureAsync(
        IEnumerable<Guid> employeeIds, CancellationToken ct = default)
    {
        var ids = employeeIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<Guid>();
        List<Guid> matches = await db.EmployeeSalaryStructures
            .Where(s => ids.Contains(s.EmployeeId) && s.EffectiveTo == null)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);
        return matches.ToHashSet();
    }

    public async Task AddAsync(EmployeeSalaryStructure structure, CancellationToken ct = default) =>
        await db.EmployeeSalaryStructures.AddAsync(structure, ct);

    public void Update(EmployeeSalaryStructure structure) =>
        db.EmployeeSalaryStructures.Update(structure);
}
