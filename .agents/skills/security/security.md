# Skill: Security — Auth, Encryption, Tenant Isolation, RBAC, Audit Trail

## Threat Model (Payroll-Specific)

1. **Cross-tenant data leakage** — highest severity. PII + salary data across orgs.
2. **PAN/Aadhaar/bank account exposure** — regulatory (IT Act, UIDAI) + reputational.
3. **Unauthorised payroll manipulation** — financial fraud.
4. **JWT forgery / token misuse** — impersonation, privilege escalation.
5. **Audit trail tampering** — compliance failure.

---

## Authentication: OpenIddict + ASP.NET Identity

### Token configuration
```csharp
services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetIntrospectionEndpointUris("/connect/introspect")
               .SetRevocationEndpointUris("/connect/revoke");

        options.AllowPasswordFlow()         // internal clients
               .AllowRefreshTokenFlow();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));  // short-lived
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));

        // Signing: asymmetric key (RS256) — not symmetric
        options.AddSigningCertificate(LoadSigningCert());
        options.AddEncryptionCertificate(LoadEncryptionCert());

        // Store tokens in DB for revocation support
        options.UseAspNetCore();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

### Custom claims in token
```csharp
// Always include tenant_id in access token
var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
identity.AddClaim(new Claim("tenant_id", tenant.Id.ToString()));
identity.AddClaim(new Claim("tenant_schema", tenant.Schema));
identity.AddClaim(new Claim(ClaimTypes.Role, user.Role));
identity.AddClaim(new Claim("sub", user.Id.ToString()));
```

---

## Tenant Isolation: Double-Lock Pattern

```
Request flow:
1. Nginx extracts subdomain → passes X-Tenant-Slug header
2. TenantResolutionMiddleware → resolves tenant from Redis-cached registry
3. ASP.NET auth middleware → validates JWT
4. TenantValidationMiddleware → JWT tenant_id claim MUST match resolved tenant
   Mismatch → 403, audit log entry, alert
5. TenantContext scoped to request → DbContextFactory uses schema
```

```csharp
// Belt-and-suspenders: after auth, before handler
public sealed class TenantClaimValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context, ITenantContext tenant, IAuditLogger audit)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var jwtTenantId = context.User.FindFirstValue("tenant_id");
            var resolvedTenantId = tenant.TenantId.ToString();

            if (jwtTenantId != resolvedTenantId)
            {
                await audit.LogSecurityEventAsync(
                    "TENANT_MISMATCH",
                    $"JWT tenant {jwtTenantId} != resolved tenant {resolvedTenantId}",
                    context.Connection.RemoteIpAddress?.ToString());

                context.Response.StatusCode = 403;
                return;
            }
        }

        await next(context);
    }
}
```

---

## Encryption: PAN, Aadhaar, Bank Account

```csharp
// Application-layer AES-256-GCM encryption
// ASP.NET Data Protection for key management (keys rotated automatically)

public sealed class AesEncryptionService(IDataProtectionProvider dataProtection)
    : IEncryptionService
{
    // Separate protectors per data type — compromise of one key doesn't expose others
    private readonly IDataProtector _panProtector =
        dataProtection.CreateProtector("Payroll.PAN");
    private readonly IDataProtector _aadhaarProtector =
        dataProtection.CreateProtector("Payroll.Aadhaar");
    private readonly IDataProtector _bankProtector =
        dataProtection.CreateProtector("Payroll.BankAccount");

    public string EncryptPAN(string pan) => _panProtector.Protect(pan);
    public string DecryptPAN(string ciphertext) => _panProtector.Unprotect(ciphertext);

    public string EncryptAadhaar(string aadhaar) => _aadhaarProtector.Protect(aadhaar);
    public string DecryptAadhaar(string ciphertext) => _aadhaarProtector.Unprotect(ciphertext);
}
```

**Key storage:** ASP.NET Data Protection keys stored in PostgreSQL (not filesystem — survives container restarts) or Azure Key Vault.

```csharp
services.AddDataProtection()
    .PersistKeysToDbContext<AuthDbContext>()  // via EF Core
    .SetApplicationName("IndianPayroll")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

---

## Data Masking in API Responses

Never return plaintext PAN/Aadhaar in list views. Full reveal = explicit authorised action only.

```csharp
// AutoMapper / manual mapping — always mask in DTO
public sealed class EmployeeSummaryDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string MaskedPAN { get; init; } = "";      // XXXXX1234F
    public string EmployeeCode { get; init; } = "";
}

// Mapping
public static EmployeeSummaryDto ToSummary(Employee e, IEncryptionService enc)
{
    var pan = enc.DecryptPAN(e.EncryptedPAN);
    return new EmployeeSummaryDto
    {
        Id = e.Id,
        FirstName = e.FirstName,
        LastName = e.LastName,
        MaskedPAN = MaskPAN(pan),   // XXXXX1234F
        EmployeeCode = e.EmployeeCode,
    };
}

// Aadhaar reveal endpoint: requires explicit role + writes audit log
[HttpPost("{id:guid}/reveal-aadhaar")]
[Authorize(Roles = "SuperAdmin,ComplianceOfficer")]
public async Task<IActionResult> RevealAadhaar(Guid id, CancellationToken ct)
{
    var result = await mediator.Send(new RevealAadhaarQuery(id), ct);
    // Handler: decrypt + write audit log entry before returning
    return result.Match<IActionResult>(Ok, _ => Forbid());
}
```

---

## RBAC

Roles defined as constants — never magic strings in code:

```csharp
public static class Roles
{
    public const string SuperAdmin       = "SuperAdmin";        // platform owner
    public const string OrgAdmin         = "OrgAdmin";          // tenant admin
    public const string HRManager        = "HRManager";         // manage employees, run payroll
    public const string AccountsManager  = "AccountsManager";   // view payroll, download reports
    public const string Employee         = "Employee";           // self-service only
}

public static class Permissions
{
    public const string PayrollRun_Create   = "payroll:run:create";
    public const string PayrollRun_Finalise = "payroll:run:finalise";
    public const string Employee_Create     = "employee:create";
    public const string Employee_ViewPAN    = "employee:view:pan";     // restricted
    public const string Aadhaar_Reveal      = "employee:reveal:aadhaar"; // most restricted
}
```

Use policy-based auth for fine-grained checks:
```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("CanRunPayroll", policy =>
        policy.RequireRole(Roles.HRManager, Roles.OrgAdmin));
    options.AddPolicy("CanRevealAadhaar", policy =>
        policy.RequireRole(Roles.SuperAdmin, Roles.ComplianceOfficer));
});
```

---

## Audit Trail

Every sensitive action writes an immutable audit log:

```csharp
public sealed record AuditEntry(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Action,           // "PAYROLL_RUN_FINALISED", "AADHAAR_REVEALED", etc.
    string EntityType,
    Guid? EntityId,
    string? OldValue,        // JSON snapshot (never PAN/Aadhaar plaintext)
    string? NewValue,        // JSON snapshot
    string? IpAddress,
    DateTimeOffset OccurredAt
);

// Audit entries are append-only — no Update/Delete on this table
// Separate audit schema: {tenant}.audit_log
// Retention: 7 years (statutory requirement)
```

```csharp
// MediatR post-processor or domain event handler
public sealed class AuditBehaviour<TRequest, TResponse>(
    IAuditLogger audit, ITenantContext tenant, ICurrentUser user)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditableCommand
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();
        await audit.LogAsync(new AuditEntry(
            Id: Guid.NewGuid(),
            TenantId: tenant.TenantId,
            UserId: user.Id,
            Action: request.AuditAction,
            EntityType: request.AuditEntityType,
            EntityId: request.AuditEntityId,
            OccurredAt: DateTimeOffset.UtcNow
        ), ct);
        return response;
    }
}
```

---

## Input Validation & Injection Prevention

- All API input validated by FluentValidation before handler runs.
- All Dapper queries parameterised — zero string interpolation with user input.
- EF Core parameterises queries automatically — never `.FromSqlRaw($"... {userInput} ...")`.
- HTML encoding for any user-supplied text rendered in frontend.
- File uploads (variable inputs CSV): validate MIME type server-side, scan for formula injection (`=`, `+`, `-`, `@` prefix in CSV cells).

---

## Security Headers (Middleware)

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";
    await next();
});
```

---

## Rate Limiting

```csharp
// Stricter limits on auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 0;
    });

    options.AddSlidingWindowLimiter("api", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 6;
    });
});

// Apply to auth endpoints
app.MapPost("/connect/token").RequireRateLimiting("auth");
```

---

## Secret Management

```yaml
# docker-compose.yml — never env vars for secrets in production
services:
  api:
    secrets:
      - db_password
      - encryption_key
      - jwt_signing_cert

secrets:
  db_password:
    file: ./secrets/db_password.txt
  encryption_key:
    file: ./secrets/encryption_key.txt
```

```csharp
// Read from /run/secrets/ at startup
var dbPassword = File.ReadAllText("/run/secrets/db_password").Trim();
```

Never: `appsettings.json` for secrets, environment variables in docker-compose for secrets, hardcoded values, `.env` files committed to git.

---

## CI Security Gates

```yaml
# .github/workflows/security.yml
- name: Check for vulnerable packages
  run: dotnet list package --vulnerable --include-transitive

- name: npm audit
  run: npm audit --audit-level=high
  working-directory: web

- name: Secret scan
  uses: gitleaks/gitleaks-action@v2

- name: CodeQL analysis
  uses: github/codeql-action/analyze@v3
  with:
    languages: csharp, javascript
```
