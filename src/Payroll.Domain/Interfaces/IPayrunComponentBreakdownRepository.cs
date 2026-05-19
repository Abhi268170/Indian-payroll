using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IPayrunComponentBreakdownRepository
{
    Task<IReadOnlyList<PayrunComponentBreakdown>> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<PayrunComponentBreakdown>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PayrunComponentBreakdown> breakdowns, CancellationToken ct = default);
    Task AddAsync(PayrunComponentBreakdown breakdown, CancellationToken ct = default);
    void Update(PayrunComponentBreakdown breakdown);
    void Remove(PayrunComponentBreakdown breakdown);
    Task RemoveRangeByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<PayrunComponentBreakdown>> GetByEmployeeAndRunIdsAsync(Guid employeeId, IEnumerable<Guid> runIds, CancellationToken ct = default);
}
