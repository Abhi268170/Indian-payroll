using Payroll.Domain.Entities;
using Payroll.Domain.Enums;
using Payroll.Domain.ValueObjects;

namespace Payroll.Domain.Interfaces;

public interface IPayrollRunRepository
{
    Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PayrollRun?> GetByPeriodAsync(PayPeriod period, CancellationToken ct = default);
    Task<PayrollRun?> GetLatestPaidAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PayrollRun>> GetHistoryAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(PayrollRun run, CancellationToken ct = default);
    void Update(PayrollRun run);
    Task<bool> ExistsForPeriodAsync(PayPeriod period, CancellationToken ct = default);
    Task<PayrollRun?> GetActiveForPeriodAsync(PayPeriod period, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetPaidIdsForFiscalYearAsync(int fiscalYear, CancellationToken ct = default);
}
