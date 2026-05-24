using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Payroll.Application.DTOs;
using Payroll.Application.Interfaces;

namespace Payroll.Infrastructure.Services;

internal sealed class RedisJobProgressService(IDistributedCache cache) : IJobProgressService
{
    private static readonly DistributedCacheEntryOptions Ttl = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    private static string Key(Guid tenantId, string jobId) =>
        $"payroll:job:{tenantId}:{jobId}";

    public async Task InitializeAsync(Guid tenantId, string jobId, int total, CancellationToken ct = default)
    {
        var entry = new JobEntry("queued", 0, total, null, null);
        await SaveAsync(tenantId, jobId, entry, ct);
    }

    public async Task UpdateAsync(Guid tenantId, string jobId, int processed, CancellationToken ct = default)
    {
        JobEntry current = await LoadAsync(tenantId, jobId, ct) ?? new("running", 0, 0, null, null);
        var entry = current with { Status = "running", Processed = processed };
        await SaveAsync(tenantId, jobId, entry, ct);
    }

    public async Task CompleteAsync(Guid tenantId, string jobId, string resultJson, CancellationToken ct = default)
    {
        JobEntry current = await LoadAsync(tenantId, jobId, ct) ?? new("completed", 0, 0, null, null);
        var entry = current with { Status = "completed", ResultJson = resultJson };
        await SaveAsync(tenantId, jobId, entry, ct);
    }

    public async Task FailAsync(Guid tenantId, string jobId, string error, CancellationToken ct = default)
    {
        JobEntry current = await LoadAsync(tenantId, jobId, ct) ?? new("failed", 0, 0, null, null);
        var entry = current with { Status = "failed", Error = error };
        await SaveAsync(tenantId, jobId, entry, ct);
    }

    public async Task<JobProgressDto?> GetAsync(Guid tenantId, string jobId, CancellationToken ct = default)
    {
        JobEntry? entry = await LoadAsync(tenantId, jobId, ct);
        if (entry is null) return null;
        return new JobProgressDto(jobId, entry.Status, entry.Processed, entry.Total, entry.ResultJson, entry.Error);
    }

    private async Task SaveAsync(Guid tenantId, string jobId, JobEntry entry, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(entry);
        await cache.SetStringAsync(Key(tenantId, jobId), json, Ttl, ct);
    }

    private async Task<JobEntry?> LoadAsync(Guid tenantId, string jobId, CancellationToken ct)
    {
        string? json = await cache.GetStringAsync(Key(tenantId, jobId), ct);
        if (json is null) return null;
        return JsonSerializer.Deserialize<JobEntry>(json);
    }

    private record JobEntry(string Status, int Processed, int Total, string? ResultJson, string? Error);
}
