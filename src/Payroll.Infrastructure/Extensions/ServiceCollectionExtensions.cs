using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Payroll.Application.Interfaces;
using Payroll.Domain.Interfaces;
using Payroll.Infrastructure.Email;
using Payroll.Infrastructure.Middleware;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Persistence.Repositories;
using Payroll.Infrastructure.Security;
using Payroll.Infrastructure.Services;
using Payroll.Infrastructure.Storage;
using StackExchange.Redis;

namespace Payroll.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenant-scoped DbContext — schema bound at construction via TenantModelCacheKeyFactory.
        // Connection string read lazily from IConfiguration so WAF test overrides are respected.
        services.AddDbContextFactory<PayrollDbContext>((sp, options) =>
        {
            string cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Payroll")
                ?? throw new InvalidOperationException("Connection string 'Payroll' not configured.");
            options.UseNpgsql(cs, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
        });

        // Platform context — public schema only (tenants, OpenIddict, Data Protection keys).
        // UseOpenIddict() is required so EF model includes OpenIddict entity configs.
        services.AddDbContext<PlatformDbContext>((sp, options) =>
        {
            string cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Payroll")
                ?? throw new InvalidOperationException("Connection string 'Payroll' not configured.");
            options.UseNpgsql(cs).UseSnakeCaseNamingConvention().UseOpenIddict();
        });

        // Data Protection keys stored in DB — survives container restarts
        services.AddDataProtection()
            .PersistKeysToDbContext<PlatformDbContext>()
            .SetApplicationName("IndianPayroll");

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, RedisTenantResolver>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrgProfileRepository, OrgProfileRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IWorkLocationRepository, WorkLocationRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDesignationRepository, DesignationRepository>();
        services.AddScoped<ICostCentreRepository, CostCentreRepository>();
        services.AddScoped<IBusinessUnitRepository, BusinessUnitRepository>();
        services.AddScoped<IPayScheduleRepository, PayScheduleRepository>();
        services.AddScoped<IPlatformUnitOfWork, PlatformUnitOfWork>();
        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisioner>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<ITenantCacheService, TenantCacheService>();

        // Redis — read lazily for same reason as DbContext
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            string redis = sp.GetRequiredService<IConfiguration>()["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis:ConnectionString not configured.");
            return ConnectionMultiplexer.Connect(redis);
        });
        services.AddStackExchangeRedisCache(options => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IConfiguration>((opts, cfg) =>
            {
                opts.Configuration = cfg["Redis:ConnectionString"]
                    ?? throw new InvalidOperationException("Redis:ConnectionString not configured.");
            });

        // Hangfire — read connection string lazily
        services.AddHangfire((sp, config) =>
        {
            string cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Payroll")
                ?? throw new InvalidOperationException("Connection string 'Payroll' not configured.");
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(cs),
                    new PostgreSqlStorageOptions { SchemaName = "hangfire" });
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = ["payroll", "reports", "notifications", "default"];
        });

        // MinIO / S3 file storage
        services.Configure<MinioOptions>(configuration.GetSection("Storage:Minio"));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            MinioOptions opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            string serviceUrl = opts.UseHttps ? $"https://{opts.Endpoint}" : $"http://{opts.Endpoint}";
            AmazonS3Config s3Config = new()
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true,
            };
            return new AmazonS3Client(new BasicAWSCredentials(opts.AccessKey, opts.SecretKey), s3Config);
        });
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        services.AddHostedService<SeedDataService>();

        // Email
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddTransient<IEmailService, SmtpEmailService>();
        services.AddTransient<IEmailJobDispatcher, HangfireEmailJobDispatcher>();

        return services;
    }
}
