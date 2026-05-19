using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Domain.Constants;
using Payroll.Infrastructure.Persistence;
using Payroll.Api.Tests.Infrastructure;
using Xunit;

namespace Payroll.Api.Tests.Employees;

[Collection("Integration")]
public sealed class EmployeeTests
{
    private readonly PayrollWebApplicationFactory _factory;

    public EmployeeTests(PostgresFixture postgres, RedisFixture redis)
    {
        _factory = new PayrollWebApplicationFactory(postgres, redis);
    }

    private static string TenantHost(string slug) => $"{slug}.payroll.localhost";

    private async Task<string> GetSuperAdminTokenAsync(HttpClient client)
    {
        FormUrlEncodedContent form = new([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "superadmin@test.local"),
            new KeyValuePair<string, string>("password", "SuperAdmin@Test1234!"),
            new KeyValuePair<string, string>("client_id", "payroll-api"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access payroll.api"),
        ]);
        HttpResponseMessage response = await client.PostAsync("/connect/token", form);
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private async Task<(Guid TenantId, string Slug)> ProvisionTenantAsync(
        HttpClient client, string superAdminToken, string displayName, string slug)
    {
        client.DefaultRequestHeaders.Authorization = new("Bearer", superAdminToken);
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/tenants", new
        {
            DisplayName = displayName,
            Slug = slug,
            AdminEmail = $"admin@{slug}.test",
        });
        response.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        client.DefaultRequestHeaders.Remove("Authorization");
        return (Guid.Parse(body!["id"].ToString()!), slug);
    }

    private async Task<string> CreateTenantUserAndGetTokenAsync(
        HttpClient client, Guid tenantId, string email, string password, string role)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            TenantId = tenantId,
            IsSuperAdmin = false,
        };
        IdentityResult result = await userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);

        FormUrlEncodedContent form = new([
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("client_id", "payroll-api"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret"),
            new KeyValuePair<string, string>("scope", "openid profile email offline_access payroll.api"),
        ]);
        HttpResponseMessage tokenResponse = await client.PostAsync("/connect/token", form);
        tokenResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? body = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return body!["access_token"].ToString()!;
    }

    private static HttpRequestMessage TenantRequest(HttpMethod method, string path, string token, string slug)
    {
        HttpRequestMessage request = new(method, path);
        request.Headers.Host = TenantHost(slug);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    private static HttpRequestMessage TenantRequest(HttpMethod method, string path, string token, string slug, object body)
    {
        HttpRequestMessage request = new(method, path)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Host = TenantHost(slug);
        request.Headers.Authorization = new("Bearer", token);
        return request;
    }

    // Org structure endpoints require OrgAdmin — use a dedicated admin token for setup
    private async Task<string> CreateOrgAdminTokenAsync(HttpClient client, Guid tenantId, string suffix)
    {
        return await CreateTenantUserAndGetTokenAsync(
            client, tenantId, $"orgadmin-{suffix}@test.local", "OrgAdmin@Test1!", Roles.OrgAdmin);
    }

    private async Task<(Guid DeptId, Guid DesigId, Guid WorkLocationId)> CreateOrgPrereqsAsync(
        HttpClient client, string orgAdminToken, string slug)
    {
        using HttpRequestMessage deptReq = TenantRequest(
            HttpMethod.Post, "/api/v1/departments", orgAdminToken, slug,
            new { Name = "Engineering", Code = (string?)null });
        HttpResponseMessage deptResp = await client.SendAsync(deptReq);
        deptResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? deptBody = await deptResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Guid deptId = Guid.Parse(deptBody!["id"].ToString()!);

        using HttpRequestMessage desigReq = TenantRequest(
            HttpMethod.Post, "/api/v1/designations", orgAdminToken, slug,
            new { Name = "Software Engineer" });
        HttpResponseMessage desigResp = await client.SendAsync(desigReq);
        desigResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? desigBody = await desigResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Guid desigId = Guid.Parse(desigBody!["id"].ToString()!);

        using HttpRequestMessage wlReq = TenantRequest(
            HttpMethod.Post, "/api/v1/work-locations", orgAdminToken, slug,
            new { Name = "HQ", State = "Maharashtra" });
        HttpResponseMessage wlResp = await client.SendAsync(wlReq);
        wlResp.EnsureSuccessStatusCode();
        Dictionary<string, object>? wlBody = await wlResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Guid workLocationId = Guid.Parse(wlBody!["id"].ToString()!);

        return (deptId, desigId, workLocationId);
    }

    [Fact]
    public async Task CreateEmployee_AsHRManager_Returns201()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Employee Corp A", "emp-corp-a");
        string orgToken = await CreateOrgAdminTokenAsync(client, tenantId, "emp-corp-a");
        string hrToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "hr@emp-corp-a.test", "HRAdmin@Test1!", Roles.HRManager);

        (Guid deptId, Guid desigId, Guid workLocationId) = await CreateOrgPrereqsAsync(client, orgToken, slug);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/employees", hrToken, slug, new
            {
                FirstName = "Rahul",
                LastName = "Sharma",
                EmployeeCode = "EMP001",
                WorkEmail = "rahul.sharma@emp-corp-a.test",
                DateOfBirth = "1990-01-15",
                Gender = "Male",
                DateOfJoining = "2024-04-01",
                EmploymentType = "FullTime",
                IsDirector = false,
                EnablePortalAccess = false,
                DepartmentId = deptId,
                DesignationId = desigId,
                WorkLocationId = workLocationId,
            });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Dictionary<string, object>? body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().ContainKey("id");
    }

    [Fact]
    public async Task CreateEmployee_InvalidWorkEmail_Returns400()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Employee Corp B", "emp-corp-b");
        string orgToken = await CreateOrgAdminTokenAsync(client, tenantId, "emp-corp-b");
        string hrToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "hr@emp-corp-b.test", "HRAdmin@Test1!", Roles.HRManager);

        (Guid deptId, Guid desigId, Guid workLocationId) = await CreateOrgPrereqsAsync(client, orgToken, slug);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Post, "/api/v1/employees", hrToken, slug, new
            {
                FirstName = "Test",
                LastName = "User",
                EmployeeCode = "EMP002",
                WorkEmail = "not-a-valid-email",
                DateOfBirth = "1990-01-15",
                Gender = "Male",
                DateOfJoining = "2024-04-01",
                EmploymentType = "FullTime",
                IsDirector = false,
                EnablePortalAccess = false,
                DepartmentId = deptId,
                DesignationId = desigId,
                WorkLocationId = workLocationId,
            });
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListEmployees_AsHRManager_Returns200WithEmployees()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Employee Corp C", "emp-corp-c");
        string orgToken = await CreateOrgAdminTokenAsync(client, tenantId, "emp-corp-c");
        string hrToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "hr@emp-corp-c.test", "HRAdmin@Test1!", Roles.HRManager);

        (Guid deptId, Guid desigId, Guid workLocationId) = await CreateOrgPrereqsAsync(client, orgToken, slug);

        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/employees", hrToken, slug, new
            {
                FirstName = "Priya",
                LastName = "Nair",
                EmployeeCode = "EMP003",
                WorkEmail = "priya.nair@emp-corp-c.test",
                DateOfBirth = "1992-06-20",
                Gender = "Female",
                DateOfJoining = "2024-01-10",
                EmploymentType = "FullTime",
                IsDirector = false,
                EnablePortalAccess = false,
                DepartmentId = deptId,
                DesignationId = desigId,
                WorkLocationId = workLocationId,
            });
        (await client.SendAsync(createReq)).EnsureSuccessStatusCode();

        using HttpRequestMessage listReq = TenantRequest(HttpMethod.Get, "/api/v1/employees", hrToken, slug);
        HttpResponseMessage listResponse = await client.SendAsync(listReq);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<Dictionary<string, object>>? employees =
            await listResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        employees.Should().HaveCount(1);
        employees![0]["employeeCode"].ToString().Should().Be("EMP003");
    }

    [Fact]
    public async Task GetEmployee_ById_Returns200()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Employee Corp D", "emp-corp-d");
        string orgToken = await CreateOrgAdminTokenAsync(client, tenantId, "emp-corp-d");
        string hrToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "hr@emp-corp-d.test", "HRAdmin@Test1!", Roles.HRManager);

        (Guid deptId, Guid desigId, Guid workLocationId) = await CreateOrgPrereqsAsync(client, orgToken, slug);

        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/employees", hrToken, slug, new
            {
                FirstName = "Amit",
                LastName = "Patel",
                EmployeeCode = "EMP004",
                WorkEmail = "amit.patel@emp-corp-d.test",
                DateOfBirth = "1988-11-05",
                Gender = "Male",
                DateOfJoining = "2023-07-01",
                EmploymentType = "FullTime",
                IsDirector = false,
                EnablePortalAccess = false,
                DepartmentId = deptId,
                DesignationId = desigId,
                WorkLocationId = workLocationId,
            });
        HttpResponseMessage createResponse = await client.SendAsync(createReq);
        createResponse.EnsureSuccessStatusCode();
        Dictionary<string, object>? created = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        string empId = created!["id"].ToString()!;

        using HttpRequestMessage getReq = TenantRequest(HttpMethod.Get, $"/api/v1/employees/{empId}", hrToken, slug);
        HttpResponseMessage getResponse = await client.SendAsync(getReq);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Dictionary<string, object>? emp = await getResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        emp!["employeeCode"].ToString().Should().Be("EMP004");
        emp["firstName"].ToString().Should().Be("Amit");
    }

    [Fact]
    public async Task GetEmployee_NonExistentId_Returns404()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantId, string slug) = await ProvisionTenantAsync(
            client, superToken, "Employee Corp E", "emp-corp-e");
        string hrToken = await CreateTenantUserAndGetTokenAsync(
            client, tenantId, "hr@emp-corp-e.test", "HRAdmin@Test1!", Roles.HRManager);

        using HttpRequestMessage request = TenantRequest(
            HttpMethod.Get, $"/api/v1/employees/{Guid.NewGuid()}", hrToken, slug);
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListEmployees_TenantIsolation_CannotSeeOtherTenantEmployees()
    {
        HttpClient client = _factory.CreateClient();
        string superToken = await GetSuperAdminTokenAsync(client);
        (Guid tenantIdA, string slugA) = await ProvisionTenantAsync(
            client, superToken, "Isolation Emp A", "isol-emp-a");
        (Guid tenantIdB, string slugB) = await ProvisionTenantAsync(
            client, superToken, "Isolation Emp B", "isol-emp-b");

        string orgTokenA = await CreateOrgAdminTokenAsync(client, tenantIdA, "isol-emp-a");
        string tokenA = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdA, "hr@isol-emp-a.test", "HRAdmin@Test1!", Roles.HRManager);
        string tokenB = await CreateTenantUserAndGetTokenAsync(
            client, tenantIdB, "hr@isol-emp-b.test", "HRAdmin@Test1!", Roles.HRManager);

        (Guid deptIdA, Guid desigIdA, Guid workLocationIdA) = await CreateOrgPrereqsAsync(client, orgTokenA, slugA);

        using HttpRequestMessage createReq = TenantRequest(
            HttpMethod.Post, "/api/v1/employees", tokenA, slugA, new
            {
                FirstName = "Isolated",
                LastName = "Employee",
                EmployeeCode = "ISOL001",
                WorkEmail = "isolated.employee@isol-emp-a.test",
                DateOfBirth = "1995-03-01",
                Gender = "Male",
                DateOfJoining = "2024-01-01",
                EmploymentType = "FullTime",
                IsDirector = false,
                EnablePortalAccess = false,
                DepartmentId = deptIdA,
                DesignationId = desigIdA,
                WorkLocationId = workLocationIdA,
            });
        (await client.SendAsync(createReq)).EnsureSuccessStatusCode();

        // Tenant B must see 0 employees
        using HttpRequestMessage listReq = TenantRequest(HttpMethod.Get, "/api/v1/employees", tokenB, slugB);
        HttpResponseMessage listResponse = await client.SendAsync(listReq);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<Dictionary<string, object>>? employees =
            await listResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        employees.Should().BeEmpty("tenant B must not see tenant A employees");
    }
}
