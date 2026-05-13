using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Payroll.Api.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Run PlatformDbContext migration — creates public schema tables
        await using ServiceProvider sp = new ServiceCollection()
            .AddDbContext<PlatformDbContext>(opts =>
                opts.UseNpgsql(ConnectionString)
                    .UseSnakeCaseNamingConvention()
                    .UseOpenIddict())
            .BuildServiceProvider();

        await using AsyncServiceScope scope = sp.CreateAsyncScope();
        PlatformDbContext db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
