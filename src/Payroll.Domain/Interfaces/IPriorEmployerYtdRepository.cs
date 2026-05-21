using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IPriorEmployerYtdRepository
{
    Task<IReadOnlyList<PriorEmployerYtd>> GetByEmployeesAndFiscalYearAsync(
        IReadOnlyList<Guid> employeeIds, int fiscalYear, CancellationToken ct = default);
}
