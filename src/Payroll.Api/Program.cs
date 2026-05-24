using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Payroll.Application.Extensions;
using Payroll.Infrastructure.Extensions;
using Payroll.Infrastructure.Middleware;
using Payroll.Infrastructure.Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool isWorkerOnly = builder.Configuration.GetValue<bool>("Hangfire:WorkerOnly");

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, isWorkerOnly);

if (!isWorkerOnly)
{
    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();

    builder.Services
        .AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<PlatformDbContext>()
        .AddDefaultTokenProviders();

    // AddIdentity defaults to cookie challenge — override to OpenIddict bearer for this API-only app.
    builder.Services.Configure<AuthenticationOptions>(options =>
    {
        options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });

    builder.Services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore().UseDbContext<PlatformDbContext>();
        })
        .AddServer(options =>
        {
            options.SetTokenEndpointUris("/connect/token");
            options.SetRevocationEndpointUris("/connect/revoke");

            options.AllowPasswordFlow();
            options.AllowRefreshTokenFlow();

            // Access tokens are self-contained RS256 JWTs (no DB hit per request).
            // Refresh tokens are DB-backed reference tokens (instantly revocable).
            options.DisableAccessTokenEncryption();
            options.UseReferenceRefreshTokens();

            options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
            options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

            options.RegisterScopes(
                OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Scopes.OfflineAccess,
                "payroll.api");

            OpenIddictServerAspNetCoreBuilder aspNetCoreOptions = options.UseAspNetCore()
                .EnableTokenEndpointPassthrough();

            if (!builder.Environment.IsProduction())
            {
                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();
                // Allow HTTP for local dev and integration tests (Testcontainers uses plain HTTP)
                aspNetCoreOptions.DisableTransportSecurityRequirement();
                // Fix issuer to a stable value so tokens issued from one Host remain valid
                // when validated from a different Host (e.g., tenant-scoped integration tests).
                options.SetIssuer(new Uri("https://localhost"));
            }
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SuperAdmin",     p => p.RequireRole("SuperAdmin"));
        options.AddPolicy("OrgAdmin",       p => p.RequireRole("OrgAdmin"));
        options.AddPolicy("HRManager",      p => p.RequireRole("HRManager", "OrgAdmin"));
        options.AddPolicy("PayrollManager", p => p.RequireRole("PayrollManager", "OrgAdmin"));
        options.AddPolicy("FinanceViewer",  p => p.RequireRole("FinanceViewer", "PayrollManager", "OrgAdmin"));
        options.AddPolicy("Employee",       p => p.RequireRole("Employee", "HRManager", "PayrollManager", "OrgAdmin"));
    });

    // Rate limiting — must be configured before app.UseRateLimiter()
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = 5;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Forward real client IP from nginx — required for per-IP rate limiting to work
    // Trust only loopback and Docker bridge (172.16-31.x/12) — not all proxies
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            System.Net.IPAddress.Parse("127.0.0.1"), 8));
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            System.Net.IPAddress.Parse("172.16.0.0"), 12));
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
            else
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://payroll.localhost:5173",
                        "http://*.payroll.localhost:5173")
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        });
    });
}

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Payroll")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!)
    .AddHangfire(options => options.MinimumAvailableServers = 1);

WebApplication app = builder.Build();

// Run platform migration before app.Run() — ensures DataProtectionKeys table exists
// before Data Protection bootstraps, and before SeedDataService fires.
// Worker-only instances skip provisioning: only the API owns schema/seed lifecycle.
using (IServiceScope scope = app.Services.CreateScope())
{
    PlatformDbContext db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await db.Database.MigrateAsync();

    if (!isWorkerOnly)
    {
        // Apply any pending tenant migrations to all existing tenant schemas.
        // New tenants get migrations via TenantSchemaProvisioner.ProvisionAsync();
        // existing tenants need this on every startup after a new migration is added.
        Payroll.Domain.Interfaces.ITenantSchemaProvisioner provisioner =
            scope.ServiceProvider.GetRequiredService<Payroll.Domain.Interfaces.ITenantSchemaProvisioner>();
        List<(string Schema, Guid Id)> tenants = await db.Tenants.Select(t => new ValueTuple<string, Guid>(t.Schema, t.Id)).ToListAsync();
        foreach ((string schema, Guid id) in tenants)
            await provisioner.ProvisionAsync(schema, id);
    }
}

if (!isWorkerOnly)
{
    app.UseSerilogRequestLogging();
    app.UseCors("frontend");

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'none'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
            "connect-src 'self'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'";
        await next();
    });

    // Forward headers from nginx before rate limiting (so RemoteIpAddress = real client IP)
    app.UseForwardedHeaders();

    app.UseRateLimiter();

    // Middleware ordering is load-bearing — do not reorder:
    // 1. TenantResolutionMiddleware — extracts slug from subdomain, sets ITenantContext (pre-auth)
    // 2. UseAuthentication — validates JWT, populates HttpContext.User
    // 3. TenantClaimValidationMiddleware — cross-checks JWT tenant_id against ITenantContext (post-auth)
    // 4. UseAuthorization — role policy enforcement
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthentication();
    app.UseMiddleware<TenantClaimValidationMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHangfireDashboard("/hangfire").RequireAuthorization("SuperAdmin");

    // Daily sweep: flip Employee.Status = Exited the day after LWD passes.
    RecurringJob.AddOrUpdate<Payroll.Infrastructure.Jobs.MarkExitedOnLwdJob>(
        "mark-exited-on-lwd",
        job => job.Execute(),
        "0 1 * * *"); // 01:00 UTC daily
}

app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
