using Payroll.Domain.Entities;
using Payroll.Domain.ValueObjects;

namespace Payroll.Domain.Interfaces;

public interface IPayrollRunRepository
{
    Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayrollRun?> GetByPeriodAsync(PayPeriod period, CancellationToken cancellationToken = default);
    Task AddAsync(PayrollRun run, CancellationToken cancellationToken = default);
}
