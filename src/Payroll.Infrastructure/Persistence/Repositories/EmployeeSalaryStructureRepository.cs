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

    public async Task AddAsync(EmployeeSalaryStructure structure, CancellationToken ct = default) =>
        await db.EmployeeSalaryStructures.AddAsync(structure, ct);

    public void Update(EmployeeSalaryStructure structure) =>
        db.EmployeeSalaryStructures.Update(structure);
}
