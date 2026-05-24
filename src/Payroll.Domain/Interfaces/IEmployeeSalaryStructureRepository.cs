using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IEmployeeSalaryStructureRepository
{
    Task<EmployeeSalaryStructure?> GetActiveAsync(Guid employeeId, CancellationToken ct = default);
    Task<EmployeeSalaryStructure?> GetActiveWithOverridesAsync(Guid employeeId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetEmployeesWithActiveStructureAsync(IEnumerable<Guid> employeeIds, CancellationToken ct = default);
    Task AddAsync(EmployeeSalaryStructure structure, CancellationToken ct = default);
    void Update(EmployeeSalaryStructure structure);
}
