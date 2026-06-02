using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Entities;
using Payroll.Domain.Interfaces;

namespace Payroll.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeDocumentRepository(PayrollDbContext db) : IEmployeeDocumentRepository
{
    public Task AddAsync(EmployeeDocument document, CancellationToken ct = default) =>
        db.EmployeeDocuments.AddAsync(document, ct).AsTask();

    public async Task<IReadOnlyList<EmployeeDocument>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await db.EmployeeDocuments
            .Where(d => d.EmployeeId == employeeId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public Task<EmployeeDocument?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.EmployeeDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);
}
