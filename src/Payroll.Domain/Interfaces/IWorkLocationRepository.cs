using Payroll.Domain.Entities;

namespace Payroll.Domain.Interfaces;

public interface IWorkLocationRepository
{
    Task<IReadOnlyList<WorkLocation>> ListAsync(CancellationToken ct = default);
    Task<WorkLocation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(WorkLocation workLocation, CancellationToken ct = default);
    void Update(WorkLocation workLocation);
    void Remove(WorkLocation workLocation);
    Task<int> GetEmployeeCountAsync(Guid id, CancellationToken ct = default);
}
