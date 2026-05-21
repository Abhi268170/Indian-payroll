using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IEmployeeFyOpeningRepository
{
    Task<EmployeeFyOpening?> GetAsync(Guid employeeId, int fiscalYear, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeFyOpening>> GetByEmployeesAndFiscalYearAsync(IEnumerable<Guid> employeeIds, int fiscalYear, CancellationToken ct = default);
    Task AddAsync(EmployeeFyOpening entity, CancellationToken ct = default);
    void Update(EmployeeFyOpening entity);
}
