using Hangfire;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Payroll.Application.Extensions;
using Payroll.Infrastructure.Extensions;
using Payroll.Infrastructure.Middleware;
using Payroll.Infrastructure.Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
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

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<PlatformDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetUserinfoEndpointUris("/connect/userinfo");
        options.SetRevocationEndpointUris("/connect/revoke");

        options.AllowPasswordFlow();
        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();

        options.UseReferenceAccessTokens();
        options.UseReferenceRefreshTokens();

        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.OfflineAccess,
            "payroll.api");


        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate();
            options.AddDevelopmentSigningCertificate();
        }

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough()
            .EnableUserinfoEndpointPassthrough();
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

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Payroll")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!)
    .AddHangfire(options => options.MinimumAvailableServers = 1);

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();


// Middleware order is critical: tenant resolution BEFORE authentication.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHangfireDashboard("/hangfire");
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program { }
