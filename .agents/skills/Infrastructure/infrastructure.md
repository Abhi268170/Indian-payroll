# Skill: Infrastructure — Docker, Hangfire, Redis, MinIO, MailHog, Observability

## Docker Compose Architecture

```
internet
    │
  nginx (80/443)
    │
  ┌─┴─────────────────┐
  api                 worker
  (ASP.NET Core)      (Hangfire)
    │                   │
  pgbouncer ────────── db (PostgreSQL 16)
    │
  redis               minio (S3)
    │
  mailhog (SMTP)
    │
  prometheus → grafana+loki
```

---

## docker-compose.yml Structure

```yaml
# docker-compose.yml — production-like baseline
# docker-compose.override.yml — local dev overrides

services:

  nginx:
    image: nginx:1.27-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/certs:/etc/nginx/certs:ro
    depends_on: [api]

  api:
    build:
      context: .
      dockerfile: src/Payroll.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__Payroll: "Host=pgbouncer;Database=payroll;..."
      Redis__ConnectionString: "redis:6379"
      MinIO__Endpoint: "minio:9000"
      Email__Host: "mailhog"
      Email__Port: "1025"
    secrets:
      - db_password
      - jwt_signing_cert
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  worker:
    build:
      context: .
      dockerfile: src/Payroll.Api/Dockerfile  # same image, different entrypoint
    command: ["dotnet", "Payroll.Api.dll", "--worker-only"]
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      Hangfire__WorkerCount: "5"
    depends_on:
      db:
        condition: service_healthy

  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: payroll
      POSTGRES_USER: payroll
      POSTGRES_PASSWORD_FILE: /run/secrets/db_password
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./db/init:/docker-entrypoint-initdb.d:ro  # create schemas
    secrets: [db_password]
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U payroll"]
      interval: 10s
      timeout: 5s
      retries: 5

  pgbouncer:
    image: bitnami/pgbouncer:1.23
    environment:
      POSTGRESQL_HOST: db
      POSTGRESQL_USERNAME: payroll
      PGBOUNCER_POOL_MODE: transaction       # transaction mode for EF Core
      PGBOUNCER_MAX_CLIENT_CONN: "200"
      PGBOUNCER_DEFAULT_POOL_SIZE: "25"
    depends_on: [db]

  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes --requirepass "${REDIS_PASSWORD}"
    volumes: [redisdata:/data]
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]

  minio:
    image: minio/minio:RELEASE.2024-01-01T00-00-00Z
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: payroll
      MINIO_ROOT_PASSWORD_FILE: /run/secrets/minio_password
    volumes: [miniodata:/data]
    secrets: [minio_password]

  mailhog:
    image: mailhog/mailhog:latest
    ports:
      - "8025:8025"   # Web UI — local only, not exposed in prod
    # SMTP on :1025 — internal only, no external port

  prometheus:
    image: prom/prometheus:v2.51.0
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheusdata:/prometheus

  grafana:
    image: grafana/grafana:10.4.0
    environment:
      GF_SECURITY_ADMIN_PASSWORD_FILE: /run/secrets/grafana_password
    volumes:
      - grafanadata:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
    depends_on: [prometheus]
    secrets: [grafana_password]

volumes:
  pgdata:
  redisdata:
  miniodata:
  prometheusdata:
  grafanadata:

secrets:
  db_password:
    file: ./secrets/db_password.txt
  jwt_signing_cert:
    file: ./secrets/jwt_signing.pfx
  minio_password:
    file: ./secrets/minio_password.txt
  grafana_password:
    file: ./secrets/grafana_password.txt
```

---

## docker-compose.override.yml (local dev)

```yaml
# Never commit secrets from here. Dev overrides only.
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: "http://+:8080"
    ports:
      - "5000:8080"   # expose directly for dev

  mailhog:
    ports:
      - "1025:1025"   # expose SMTP locally for debugging

  db:
    ports:
      - "5432:5432"   # expose Postgres for local tools (DBeaver, pgAdmin)

  redis:
    ports:
      - "6379:6379"

  minio:
    ports:
      - "9000:9000"
      - "9001:9001"   # MinIO console
```

---

## Hangfire: Background Jobs

### Registration
```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connStr), new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire",        // own schema — not in tenant schemas
        DistributedLockTimeout = TimeSpan.FromMinutes(10),
    }));

services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = ["payroll", "reports", "notifications", "default"];
});
```

### Job queues and priorities
```csharp
public static class JobQueues
{
    public const string Payroll       = "payroll";       // highest priority
    public const string Reports       = "reports";       // Form 16, ECR generation
    public const string Notifications = "notifications"; // email dispatch
    public const string Default       = "default";
}
```

### Payroll run job
```csharp
public sealed class RunPayrollJob(
    IPayrollInputLoader inputLoader,
    IStatutoryConfigLoader configLoader,
    IPayrollResultWriter resultWriter,
    IEmailService email,
    ILogger<RunPayrollJob> logger)
{
    [Queue(JobQueues.Payroll)]
    public async Task ExecuteAsync(Guid payrollRunId, Guid tenantId)
    {
        // Distributed lock: one run per tenant at a time
        // Lock acquired before this job is enqueued — see PayrollRunService

        logger.LogInformation("Starting payroll run {RunId} for tenant {TenantId}",
            payrollRunId, tenantId);

        var inputs = await inputLoader.LoadAsync(payrollRunId);
        var config = await configLoader.LoadAsync(tenantId, inputs.FiscalYear);

        // Pure engine — no I/O
        var results = PayrollEngine.Compute(inputs.Employees, inputs.RunInput, config);

        // Bulk-insert results
        await resultWriter.BulkWriteAsync(payrollRunId, results);

        await email.SendPayrollCompletedAsync(inputs.NotifyEmail, results.Count);

        logger.LogInformation("Payroll run {RunId} complete: {Count} employees",
            payrollRunId, results.Count);
    }
}
```

### Redis distributed lock (prevent duplicate runs)
```csharp
public sealed class PayrollRunService(
    IConnectionMultiplexer redis,
    IBackgroundJobClient jobs)
{
    public async Task<Result<Guid>> InitiateRunAsync(
        Guid tenantId, PayPeriod period, CancellationToken ct)
    {
        var lockKey = $"payroll:run:lock:{tenantId}:{period}";
        var db = redis.GetDatabase();

        // Try to acquire lock — 35 min TTL (30 min SLA + 5 min buffer)
        var acquired = await db.StringSetAsync(
            lockKey, "1",
            TimeSpan.FromMinutes(35),
            When.NotExists);

        if (!acquired)
            return Result.Fail<Guid>(new Error("A payroll run is already in progress"));

        var runId = Guid.NewGuid();
        jobs.Enqueue<RunPayrollJob>(q => q.ExecuteAsync(runId, tenantId));
        return Result.Ok(runId);
    }
}
```

---

## Redis Usage Patterns

```csharp
// 1. Tenant registry cache — resolve tenant from subdomain
public sealed class RedisTenantCache(IConnectionMultiplexer redis)
{
    private static string Key(string slug) => $"tenant:{slug}";

    public async Task<TenantInfo?> GetAsync(string slug)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(Key(slug));
        return value.IsNullOrEmpty ? null
            : JsonSerializer.Deserialize<TenantInfo>(value!);
    }

    public async Task SetAsync(TenantInfo tenant)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(
            Key(tenant.Slug),
            JsonSerializer.Serialize(tenant),
            TimeSpan.FromMinutes(30));
    }
}

// 2. Output caching for expensive reads (payroll summary, reports)
services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = config["Redis:ConnectionString"];
});

// 3. Distributed lock (see payroll run above)

// 4. Session store (OpenIddict tokens backed by Redis for fast revocation check)
```

---

## MinIO: File Storage

```csharp
public sealed class MinIOStorageService(IAmazonS3 s3, IConfiguration config)
    : IStorageService
{
    private readonly string _bucket = config["MinIO:BucketName"]!;

    // Store variable inputs file — immutable artifact per payroll run
    public async Task<string> StoreVariableInputsAsync(
        Guid tenantId, Guid runId, Stream fileStream, string fileName)
    {
        var key = $"{tenantId}/variable-inputs/{runId}/{fileName}";
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = fileStream,
            ContentType = "text/csv",
            // Object lock: immutable for 7 years (statutory)
            ObjectLockMode = ObjectLockMode.COMPLIANCE,
            ObjectLockRetainUntilDate = DateTime.UtcNow.AddYears(7),
        });
        return key;
    }

    // Presigned URL for payslip download — 15 min TTL
    public async Task<string> GetPresignedUrlAsync(string key)
    {
        return await s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(15),
            Verb = HttpVerb.GET,
        });
    }
}
```

Bucket structure:
```
payroll-{env}/
  {tenantId}/
    variable-inputs/{runId}/{filename}    # immutable compliance artifact
    payslips/{runId}/{employeeId}.pdf     # generated payslips
    form16/{fy}/{employeeId}.pdf          # Form 16 PDFs
    ecr/{runId}/ecr_upload.txt            # EPFO ECR file
    exports/{reportId}/summary.xlsx       # bulk exports
```

---

## MailHog: Local SMTP

```csharp
// Email service — same interface, different impl per environment
public interface IEmailService
{
    Task SendPayslipAsync(string to, string employeeName, byte[] payslipPdf);
    Task SendPayrollCompletedAsync(string to, int employeeCount);
    Task SendOTPAsync(string to, string otp);
}

// appsettings.Development.json
{
  "Email": {
    "Host": "mailhog",     // "mailhog" when running in Docker, "localhost" for local dotnet run
    "Port": 1025,
    "UseSsl": false,
    "FromAddress": "payroll@local.dev",
    "FromName": "Indian Payroll (Dev)"
  }
}
```

MailHog UI: http://localhost:8025 — view all outgoing emails in dev.

Prod swap: Postmark or AWS SES — same config shape:
```json
{
  "Email": {
    "Host": "smtp.postmarkapp.com",
    "Port": 587,
    "UseSsl": true,
    "ApiKey": ""  // from Docker secret
  }
}
```

---

## Health Checks

```csharp
services.AddHealthChecks()
    .AddNpgsql(connStr, name: "postgres", tags: ["db"])
    .AddRedis(redisConnStr, name: "redis", tags: ["cache"])
    .AddHangfire(options => options.MinimumAvailableServers = 1, name: "hangfire")
    .AddMinio(name: "minio", tags: ["storage"]);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // liveness: just HTTP 200
});
```

---

## Observability

### Prometheus metrics
```csharp
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddRuntimeInstrumentation()
               .AddPrometheusExporter();

        // Custom payroll metrics
        metrics.AddMeter("Payroll.Engine");
        metrics.AddMeter("Payroll.Jobs");
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddEntityFrameworkCoreInstrumentation()
               .AddRedisInstrumentation()
               .AddOtlpExporter();
    });
```

Custom meters:
```csharp
// In RunPayrollJob
_payrollRunsTotal.Add(1, new("tenant", tenantId), new("status", "completed"));
_payrollDuration.Record(elapsed.TotalSeconds, new("tenant", tenantId));
_employeesProcessed.Add(results.Count, new("tenant", tenantId));
```

### Grafana dashboards (provision via JSON)
Key dashboards:
- Payroll run throughput + latency (p50, p95, p99)
- Employee count per tenant
- Failed job queue depth (alert if > 0)
- DB connection pool utilisation (PgBouncer)
- Redis memory + hit rate

### Loki structured logging
```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithTenantId()       // custom enricher from TenantContext
    .Enrich.FromLogContext()
    .WriteTo.GrafanaLoki("http://loki:3100", new[] {
        new LokiLabel { Key = "app", Value = "payroll-api" },
        new LokiLabel { Key = "env", Value = env.EnvironmentName },
    })
    .CreateLogger();
```

**Never log:** PAN, Aadhaar, bank account, raw salary figures in logs — these fields are PII. Log employee IDs and run IDs only.

---

## Nginx Config

```nginx
# nginx/nginx.conf
server {
    listen 80;
    server_name ~^(?<tenant>[^.]+)\.yourdomain\.com$;

    # Pass tenant slug to API
    location /api/ {
        proxy_pass http://api:8080;
        proxy_set_header X-Tenant-Slug $tenant;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # Static React app
    location / {
        proxy_pass http://api:8080;  # or separate static server
    }

    # Hangfire dashboard — internal only
    location /hangfire {
        allow 10.0.0.0/8;   # internal network only
        deny all;
        proxy_pass http://api:8080;
    }
}
```

---

## CI/CD — GitHub Actions

```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env: { POSTGRES_PASSWORD: test }
        options: --health-cmd pg_isready
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore -warnaserror
      - run: dotnet format --verify-no-changes
      - run: dotnet test --no-build --collect:"XPlat Code Coverage"
      - run: dotnet list package --vulnerable --include-transitive

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: npm ci
        working-directory: web
      - run: npm run typecheck
        working-directory: web
      - run: npm run lint
        working-directory: web
      - run: npm run test:coverage
        working-directory: web

  e2e:
    runs-on: ubuntu-latest
    needs: [backend, frontend]
    steps:
      - uses: actions/checkout@v4
      - run: docker compose up -d --wait
      - run: npx playwright test
      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: playwright-report/

  deploy:
    runs-on: ubuntu-latest
    needs: [backend, frontend, e2e]
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy to VPS
        run: |
          ssh deploy@${{ secrets.VPS_HOST }} '
            cd /opt/payroll &&
            git pull &&
            docker compose pull &&
            docker compose up -d --remove-orphans
          '
```
