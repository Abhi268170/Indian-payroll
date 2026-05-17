using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface ITdsWorksheetRepository
{
    Task<TdsWorksheet?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<TdsWorksheet>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
    Task AddAsync(TdsWorksheet worksheet, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TdsWorksheet> worksheets, CancellationToken ct = default);
}
