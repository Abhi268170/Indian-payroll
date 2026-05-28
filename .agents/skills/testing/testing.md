# Skill: Testing — xUnit, Testcontainers, Vitest, Playwright

## Philosophy

TDD on payroll engine: non-negotiable. Statutory bugs = legal liability.
Red → Green → Refactor. Every statutory scenario has a test with exact expected values.

---

## Test Project Structure

```
tests/
  Payroll.Engine.Tests/
    Calculators/
      TDSCalculatorTests.cs
      PFCalculatorTests.cs
      ESICalculatorTests.cs
      PTCalculatorTests.cs
      LWFCalculatorTests.cs
      GrossCalculatorTests.cs
    Integration/
      PayrollEngineIntegrationTests.cs   # full run: all calculators together
    TestData/
      StatutoryTestData.cs               # FY-specific slabs/config for tests
      EmployeeFixtures.cs                # Bogus-based test employee builders
  Payroll.Application.Tests/
    Handlers/
      CreateEmployeeHandlerTests.cs
      RunPayrollHandlerTests.cs
    Validators/
      CreateEmployeeCommandValidatorTests.cs
    Behaviours/
      ValidationBehaviourTests.cs
  Payroll.Infrastructure.Tests/          # Testcontainers: real PostgreSQL
    Repositories/
      EmployeeRepositoryTests.cs
      PayrollRunRepositoryTests.cs
    Migrations/
      MigrationRunnerTests.cs
  Payroll.Api.Tests/                     # WebApplicationFactory + real DB
    Endpoints/
      EmployeesEndpointTests.cs
      PayrollRunEndpointTests.cs
    Auth/
      TenantIsolationTests.cs            # cross-tenant access = 403
web/src/
  features/*/
    *.test.tsx                           # co-located with component
e2e/
  payroll-run.spec.ts
  employee-management.spec.ts
  auth.spec.ts
```

---

## xUnit Patterns

### Engine Test — exact decimal assertions
```csharp
public sealed class TDSCalculatorTests
{
    private static readonly IReadOnlyList<TaxSlab> Slabs =
        StatutoryTestData.NewRegimeSlabs_FY2026();

    [Theory]
    [MemberData(nameof(TDSScenarios))]
    public void Compute_ReturnsCorrectMonthlyTDS(
        decimal annualGross,
        decimal priorTDS,
        decimal priorTaxableIncome,
        int monthsRemaining,
        decimal expectedAnnualTax,
        decimal expectedMonthlyTDS)
    {
        var result = TDSCalculator.Compute(
            annualGross, priorTDS, priorTaxableIncome, Slabs, monthsRemaining);

        result.AnnualTaxLiability.Should().Be(expectedAnnualTax);
        result.MonthlyTDS.Should().Be(expectedMonthlyTDS);
    }

    public static TheoryData<decimal, decimal, decimal, int, decimal, decimal> TDSScenarios =>
        new()
        {
            // Income ≤ ₹7L after std deduction → rebate 87A → zero tax
            { 650_000m, 0m, 0m, 12, 0m, 0m },
            // ₹8L income → tax on ₹5,25,000 (after ₹75k std deduction)
            { 800_000m, 0m, 0m, 12, 11_250m, 938m },
            // Mid-year joiner: 6 months remaining, prior TDS ₹5000 already paid
            { 600_000m, 5_000m, 250_000m, 6, 0m, 0m },
        };
}
```

### Application Handler Test — mock infrastructure, test business logic
```csharp
public sealed class CreateEmployeeHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly IEncryptionService _encryption = Substitute.For<IEncryptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ValidCommand_CreatesEmployeeAndReturnsId()
    {
        _tenant.TenantId.Returns(Guid.NewGuid());
        _encryption.Encrypt("ABCDE1234F").Returns("enc-pan");

        var handler = new CreateEmployeeHandler(
            _employees, _tenant, _encryption, _uow);

        var command = new CreateEmployeeCommandBuilder().Build();
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _employees.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.EmployeeCode != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AadhaarMissing_StillCreatesEmployee_WithWarning()
    {
        // Aadhaar optional — warn but don't block
        var command = new CreateEmployeeCommandBuilder()
            .WithAadhaar(null)
            .Build();

        var result = await new CreateEmployeeHandler(
            _employees, _tenant, _encryption, _uow)
            .Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
```

---

## Testcontainers — Integration Tests

```csharp
// Shared container across test collection — one PostgreSQL per test run
[CollectionDefinition("PostgreSQL")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("payroll_test")
        .WithUsername("payroll")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Run migrations against test DB
        await RunMigrationsAsync(ConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private static async Task RunMigrationsAsync(string connStr)
    {
        // EF Core migration runner or Respawn reset
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseNpgsql(connStr)
            .Options;
        await using var ctx = new PayrollDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
    }
}

[Collection("PostgreSQL")]
public sealed class EmployeeRepositoryTests(PostgresFixture db)
{
    [Fact]
    public async Task AddAsync_PersistsEmployee_CanRetrieveById()
    {
        var tenantCtx = new TestTenantContext("test_tenant");
        var ctxFactory = new TestTenantDbContextFactory(db.ConnectionString, tenantCtx);
        var repo = new EmployeeRepository(ctxFactory);

        var employee = EmployeeFixtures.Valid();
        await repo.AddAsync(employee, CancellationToken.None);

        var retrieved = await repo.GetByIdAsync(employee.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(employee.Id);
    }
}
```

---

## Tenant Isolation Test — Critical

```csharp
[Collection("PostgreSQL")]
public sealed class TenantIsolationTests(PostgresFixture db)
{
    [Fact]
    public async Task EmployeeInTenant1_InvisibleToTenant2_Queries()
    {
        // Arrange: two tenants, each with employees
        var tenant1 = await CreateTenantWithEmployeeAsync(db, "tenant_alpha");
        var tenant2 = await CreateTenantWithEmployeeAsync(db, "tenant_beta");

        // Act: query tenant2's repo for tenant1's employee ID
        var repo2 = BuildRepoForTenant(db, "tenant_beta");
        var result = await repo2.GetByIdAsync(tenant1.EmployeeId, CancellationToken.None);

        // Assert: must return null — no cross-tenant data leakage
        result.Should().BeNull();
    }
}
```

---

## WebApplicationFactory — API Tests

```csharp
public sealed class PayrollApiFactory : WebApplicationFactory<Program>
{
    private readonly PostgresFixture _db;

    public PayrollApiFactory(PostgresFixture db) { _db = db; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real DB with test DB
            services.RemoveAll<DbContextOptions<PayrollDbContext>>();
            services.AddDbContextFactory<PayrollDbContext>(opts =>
                opts.UseNpgsql(_db.ConnectionString));

            // Replace MailHog with no-op email
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, NoOpEmailService>();
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid tenantId, string role = "Admin")
    {
        var client = CreateClient();
        var token = TestJwtFactory.CreateToken(tenantId, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class EmployeesEndpointTests(PostgresFixture db)
    : IClassFixture<PayrollApiFactory>
{
    [Fact]
    public async Task POST_employees_ValidRequest_Returns201WithId()
    {
        using var factory = new PayrollApiFactory(db);
        var client = factory.CreateAuthenticatedClient(TestTenants.Alpha.Id);

        var payload = new CreateEmployeeRequest(
            FirstName: "Rahul",
            LastName: "Sharma",
            PAN: "ABCDE1234F",
            DateOfJoining: "2026-04-01");

        var response = await client.PostAsJsonAsync("/api/v1/employees", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task POST_employees_InvalidPAN_Returns400WithValidationError()
    {
        using var factory = new PayrollApiFactory(db);
        var client = factory.CreateAuthenticatedClient(TestTenants.Alpha.Id);

        var response = await client.PostAsJsonAsync("/api/v1/employees",
            new { firstName = "Rahul", pan = "INVALID" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("pan");
    }
}
```

---

## Test Data Builders

```csharp
// Builder pattern — readable, flexible test data construction
public sealed class CreateEmployeeCommandBuilder
{
    private string _firstName = "Rahul";
    private string _lastName = "Sharma";
    private string _pan = "ABCDE1234F";
    private string? _aadhaar = "123456789012";
    private DateOnly _dateOfJoining = DateOnly.FromDateTime(DateTime.Today);

    public CreateEmployeeCommandBuilder WithAadhaar(string? aadhaar)
    {
        _aadhaar = aadhaar;
        return this;
    }

    public CreateEmployeeCommandBuilder WithPAN(string pan)
    {
        _pan = pan;
        return this;
    }

    public CreateEmployeeCommand Build() =>
        new(_firstName, _lastName, _pan, _aadhaar,
            Guid.NewGuid(), Guid.NewGuid(), _dateOfJoining);
}

// Bogus for realistic bulk test data
public static class EmployeeFixtures
{
    private static readonly Faker<Employee> Faker = new Faker<Employee>("en_IN")
        .RuleFor(e => e.Id, _ => Guid.NewGuid())
        .RuleFor(e => e.FirstName, f => f.Name.FirstName())
        .RuleFor(e => e.LastName, f => f.Name.LastName())
        .RuleFor(e => e.EmployeeCode, f => $"EMP{f.Random.Number(1000, 9999)}")
        .RuleFor(e => e.State, f => f.PickRandom("KA", "MH", "TN", "DL"));

    public static Employee Valid() => Faker.Generate();
    public static List<Employee> Many(int count) => Faker.Generate(count);
}
```

---

## Vitest — Component Tests

```typescript
// features/employees/CreateEmployeeForm.test.tsx
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { server } from '@/test/server'   // MSW server setup
import { CreateEmployeeForm } from './CreateEmployeeForm'
import { TestProviders } from '@/test/TestProviders'

describe('CreateEmployeeForm', () => {
  it('shows validation error for invalid PAN', async () => {
    render(<CreateEmployeeForm />, { wrapper: TestProviders })
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/pan/i), 'INVALID')
    await user.click(screen.getByRole('button', { name: /create employee/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid PAN format')
  })

  it('submits valid form and shows success', async () => {
    server.use(
      http.post('/api/v1/employees', () =>
        HttpResponse.json('550e8400-e29b-41d4-a716-446655440000', { status: 201 })
      )
    )

    render(<CreateEmployeeForm />, { wrapper: TestProviders })
    const user = userEvent.setup()

    await user.type(screen.getByLabelText(/first name/i), 'Rahul')
    await user.type(screen.getByLabelText(/pan/i), 'ABCDE1234F')
    await user.click(screen.getByRole('button', { name: /create employee/i }))

    await waitFor(() =>
      expect(screen.queryByDisplayValue('Rahul')).not.toBeInTheDocument()
    )
  })
})
```

---

## Playwright — E2E

```typescript
// e2e/payroll-run.spec.ts
import { test, expect } from '@playwright/test'
import { loginAs } from './helpers/auth'
import { seedTenant } from './helpers/db'

test.describe('Payroll Run Flow', () => {
  test.beforeEach(async ({ page }) => {
    await seedTenant('test_tenant')
    await loginAs(page, 'admin@acme.com', 'Test@1234')
  })

  test('create and finalise payroll run for April 2026', async ({ page }) => {
    await page.goto('/payroll/runs/new')

    await page.getByLabel('Pay Period').selectOption('April 2026')
    await page.getByRole('button', { name: 'Initiate Run' }).click()

    // Wait for background job to complete (Hangfire)
    await expect(page.getByTestId('run-status')).toHaveText('Completed', {
      timeout: 60_000
    })

    // Verify payslip count
    await expect(page.getByTestId('employee-count')).toContainText('employees processed')

    // Download summary
    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByRole('button', { name: 'Download Summary' }).click(),
    ])
    expect(download.suggestedFilename()).toMatch(/payroll-summary.*\.xlsx/)
  })
})
```

---

## Coverage Configuration

```json
// vitest.config.ts
{
  coverage: {
    provider: 'v8',
    thresholds: {
      branches: 70,
      functions: 75,
      lines: 75,
    },
    exclude: ['**/*.test.tsx', '**/test/**', 'src/types/**']
  }
}
```

```xml
<!-- In Payroll.Engine.Tests.csproj -->
<PackageReference Include="coverlet.collector" />
<!-- CI: dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=95 -->
```

---

## Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedResult]
[EntityOrFeature]_[Scenario]_[ExpectedResult]

Examples:
  TDS_IncomeUnder700k_Rebate87AApplied
  PF_SalaryAboveCap_CalculatesOnMaxWage
  CreateEmployee_InvalidPAN_ReturnsValidationError
  TenantIsolation_CrossTenantEmployeeQuery_ReturnsNull
```
