using Payroll.Domain.Entities;
using Payroll.Domain.Enums;

namespace Payroll.Domain.Interfaces;

public interface IPayrunEmployeeRepository
{
    Task<IReadOnlyList<PayrunEmployee>> GetByRunIdAsync(Guid payrollRunId, CancellationToken ct = default);
    Task<PayrunEmployee?> GetByRunAndEmployeeAsync(Guid payrollRunId, Guid employeeId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PayrunEmployee> employees, CancellationToken ct = default);
    Task AddAsync(PayrunEmployee employee, CancellationToken ct = default);
    void Update(PayrunEmployee employee);
    void Remove(PayrunEmployee employee);
    Task<IReadOnlyList<PayrunEmployee>> GetByRunIdWithStatusAsync(Guid payrollRunId, PayrunEmployeeStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<PayrunEmployee>> GetByEmployeeAndRunIdsAsync(Guid employeeId, IEnumerable<Guid> runIds, CancellationToken ct = default);
    Task<Dictionary<Guid, (decimal YtdGross, decimal YtdTaxableGross, decimal YtdTds)>> GetCurrentEmployerYtdAsync(IEnumerable<Guid> employeeIds, int fiscalYear, CancellationToken ct = default);

    // Returns true if any Approved/Paid run between [firstMonth..lastMonth] of `year`
    // recorded a non-zero LWF amount for the employee. Drives FnF's half-year
    // duplicate-protection: LWF that already hit a prior month must not be deducted
    // again in the closing run.
    Task<bool> HasLwfDeductedInPeriodAsync(Guid employeeId, int year, int firstMonth, int lastMonth, CancellationToken ct = default);
}
