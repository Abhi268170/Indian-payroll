using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using Payroll.Application.DTOs;
using Payroll.Infrastructure.Services;
using Testcontainers.Redis;
using Xunit;

namespace Payroll.Infrastructure.Tests.Services;

public sealed class RedisJobProgressServiceTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().Build();
    private RedisJobProgressService _svc = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        IOptions<RedisCacheOptions> opts = Options.Create(new RedisCacheOptions
        {
            Configuration = _redis.GetConnectionString()
        });
        IDistributedCache cache = new RedisCache(opts);
        _svc = new RedisJobProgressService(cache);
    }

    public async Task DisposeAsync() => await _redis.DisposeAsync();

    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task InitializeAsync_GetAsync_ReturnsQueuedState()
    {
        string jobId = Guid.NewGuid().ToString();
        await _svc.InitializeAsync(TenantId, jobId, 100);

        JobProgressDto? dto = await _svc.GetAsync(TenantId, jobId);

        dto.Should().NotBeNull();
        dto!.Status.Should().Be("queued");
        dto.Processed.Should().Be(0);
        dto.Total.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_ReflectsProgress()
    {
        string jobId = Guid.NewGuid().ToString();
        await _svc.InitializeAsync(TenantId, jobId, 200);
        await _svc.UpdateAsync(TenantId, jobId, 75);

        JobProgressDto? dto = await _svc.GetAsync(TenantId, jobId);

        dto!.Status.Should().Be("running");
        dto.Processed.Should().Be(75);
        dto.Total.Should().Be(200);
    }

    [Fact]
    public async Task CompleteAsync_SetsStatusAndResult()
    {
        string jobId = Guid.NewGuid().ToString();
        await _svc.InitializeAsync(TenantId, jobId, 10);
        await _svc.CompleteAsync(TenantId, jobId, "{\"applied\":10}");

        JobProgressDto? dto = await _svc.GetAsync(TenantId, jobId);

        dto!.Status.Should().Be("completed");
        dto.ResultJson.Should().Be("{\"applied\":10}");
    }

    [Fact]
    public async Task FailAsync_SetsStatusAndError()
    {
        string jobId = Guid.NewGuid().ToString();
        await _svc.InitializeAsync(TenantId, jobId, 10);
        await _svc.FailAsync(TenantId, jobId, "Something went wrong");

        JobProgressDto? dto = await _svc.GetAsync(TenantId, jobId);

        dto!.Status.Should().Be("failed");
        dto.Error.Should().Be("Something went wrong");
    }

    [Fact]
    public async Task GetAsync_UnknownJobId_ReturnsNull()
    {
        JobProgressDto? dto = await _svc.GetAsync(TenantId, Guid.NewGuid().ToString());
        dto.Should().BeNull();
    }
}
