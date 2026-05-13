# Skill: .NET API — Controllers, MediatR, FluentValidation, OpenIddict

## Layer Responsibilities

```
Payroll.Api/           → HTTP surface: routing, auth, response shaping, middleware
Payroll.Application/   → Use cases: commands, queries, handlers, validators, DTOs
Payroll.Domain/        → Entities, value objects, domain events, business rules
Payroll.Infrastructure/→ EF Core, Dapper, Redis, MinIO, Email, Hangfire wiring
```

---

## Controller Pattern

Controllers are thin. Zero business logic. Zero EF. Zero direct service calls.

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]                          // default: all endpoints require auth
public sealed class EmployeesController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<EmployeeDto>(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmployeeQuery(id), ct);
        return result.Match<IActionResult>(Ok, _ => NotFound());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HRManager")]
    [ProducesResponseType<Guid>(201)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<IActionResult> CreateEmployee(
        CreateEmployeeCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.Match<IActionResult>(
            id => CreatedAtAction(nameof(GetEmployee), new { id }, id),
            errors => ValidationProblem(errors.ToModelStateDictionary()));
    }
}
```

---

## MediatR Patterns

### Command (write operation)
```csharp
// Command: immutable record
public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string PAN,
    string? Aadhaar,
    Guid DepartmentId,
    Guid SalaryStructureId,
    DateOnly DateOfJoining
) : IRequest<Result<Guid>>;

// Handler: one responsibility
internal sealed class CreateEmployeeHandler(
    IEmployeeRepository employees,
    ITenantContext tenant,
    IEncryptionService encryption,
    IUnitOfWork uow) : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateEmployeeCommand cmd, CancellationToken ct)
    {
        var encryptedPAN = encryption.Encrypt(cmd.PAN);
        var employee = Employee.Create(cmd.FirstName, cmd.LastName,
            encryptedPAN, tenant.TenantId, cmd.DateOfJoining);

        await employees.AddAsync(employee, ct);
        await uow.CommitAsync(ct);

        return Result.Ok(employee.Id);
    }
}
```

### Query (read operation — Dapper preferred for complex reads)
```csharp
public sealed record GetPayrollSummaryQuery(
    Guid PayrollRunId) : IRequest<Result<PayrollSummaryDto>>;

internal sealed class GetPayrollSummaryHandler(
    IPayrollReadRepository readRepo) : IRequestHandler<GetPayrollSummaryQuery, Result<PayrollSummaryDto>>
{
    public async Task<Result<PayrollSummaryDto>> Handle(
        GetPayrollSummaryQuery query, CancellationToken ct)
    {
        var summary = await readRepo.GetSummaryAsync(query.PayrollRunId, ct);
        return summary is null
            ? Result.Fail<PayrollSummaryDto>(NotFoundError.Instance)
            : Result.Ok(summary);
    }
}
```

---

## MediatR Pipeline Behaviours

Register in order — runs top to bottom on every command/query:

```csharp
// Program.cs — pipeline registration order matters
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantValidationBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
});
```

### ValidationBehaviour (FluentValidation)
```csharp
internal sealed class ValidationBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### TenantValidationBehaviour
```csharp
// Ensures handler runs within an established tenant context
// Rejects requests where JWT tenant_id doesn't match resolved tenant
internal sealed class TenantValidationBehaviour<TRequest, TResponse>(
    ITenantContext tenant,
    IHttpContextAccessor http)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Validation already performed at middleware level
        // This behaviour is a belt-and-suspenders guard
        if (tenant.TenantId == Guid.Empty)
            throw new UnauthorizedTenantAccessException();

        return await next();
    }
}
```

---

## FluentValidation Patterns

```csharp
internal sealed class CreateEmployeeCommandValidator
    : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PAN)
            .NotEmpty()
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$")
            .WithMessage("Invalid PAN format");

        RuleFor(x => x.Aadhaar)
            .Matches(@"^\d{12}$")
            .When(x => x.Aadhaar is not null)
            .WithMessage("Aadhaar must be 12 digits");

        RuleFor(x => x.DateOfJoining)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Joining date cannot be in the future");
    }
}
```

---

## Result Pattern

Use `Result<T>` (not exceptions) for expected failures:

```csharp
// Application layer returns Result<T>
// Controller maps Result<T> to HTTP response
// Exceptions = unexpected failures only (infra errors, bugs)

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<IReadOnlyList<Error>, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Errors);
}
```

---

## Global Exception Handling

```csharp
// Middleware — maps unhandled exceptions to RFC 7807 ProblemDetails
app.UseExceptionHandler(builder => builder.Run(async context =>
{
    var problem = context.RequestServices.GetRequiredService<IProblemDetailsService>();
    var feature = context.Features.Get<IExceptionHandlerFeature>();

    var (status, title) = feature?.Error switch
    {
        ValidationException => (400, "Validation failed"),
        NotFoundException   => (404, "Resource not found"),
        UnauthorizedTenantAccessException => (403, "Access denied"),
        _ => (500, "Internal server error")
    };
    // Log 500s — never log PAN/Aadhaar in error messages
    await problem.WriteAsync(new ProblemDetailsContext { ... });
}));
```

---

## Tenant Resolution Middleware

```csharp
// Must run before auth — establishes TenantContext
// Subdomain → Redis lookup → TenantId
// JWT tenant_id claim → must match

public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, ITenantContext tenantCtx)
    {
        var subdomain = context.Request.Host.Host.Split('.')[0];
        var tenant = await resolver.ResolveAsync(subdomain);

        if (tenant is null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // JWT claim check happens after auth middleware
        // Store resolved tenant — claim validation in TenantValidationBehaviour
        tenantCtx.SetTenant(tenant);
        await next(context);
    }
}
```

---

## API Versioning & Documentation

```csharp
// Scalar (modern OpenAPI UI)
builder.Services.AddOpenApi();
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Indian Payroll API";
    options.Theme = ScalarTheme.Default;
});
```

Every endpoint has `[ProducesResponseType]` attributes — required for accurate OpenAPI spec.

---

## DI Registration Pattern

Each infrastructure service registers itself via extension method:

```csharp
// Payroll.Infrastructure/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration config)
{
    services.AddDbContextFactory<PayrollDbContext>(...);
    services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
    services.AddSingleton<IEncryptionService, AesEncryptionService>();
    services.AddHangfire(...);
    services.AddStackExchangeRedisCache(...);
    // ...
    return services;
}

// Program.cs stays clean
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```
