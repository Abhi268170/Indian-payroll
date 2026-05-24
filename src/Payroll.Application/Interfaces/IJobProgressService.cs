using Payroll.Application.DTOs;

namespace Payroll.Application.Interfaces;

public interface IJobProgressService
{
    Task InitializeAsync(Guid tenantId, string jobId, int total, CancellationToken ct = default);
    Task UpdateAsync(Guid tenantId, string jobId, int processed, CancellationToken ct = default);
    Task CompleteAsync(Guid tenantId, string jobId, string resultJson, CancellationToken ct = default);
    Task FailAsync(Guid tenantId, string jobId, string error, CancellationToken ct = default);
    Task<JobProgressDto?> GetAsync(Guid tenantId, string jobId, CancellationToken ct = default);
}
