using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Middleware;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Security;
using StackExchange.Redis;

namespace Payroll.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Payroll")
            ?? throw new InvalidOperationException("Connection string 'Payroll' not configured.");
        string? redisConnection = configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString not configured.");

        // Tenant-scoped DbContext — schema bound at construction via TenantModelCacheKeyFactory
        services.AddDbContextFactory<PayrollDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        });

        // Platform context — public schema only (tenants, OpenIddict, etc.)
        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, RedisTenantResolver>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
        });

        // Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { SchemaName = "hangfire" }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = ["payroll", "reports", "notifications", "default"];
        });

        return services;
    }
}
