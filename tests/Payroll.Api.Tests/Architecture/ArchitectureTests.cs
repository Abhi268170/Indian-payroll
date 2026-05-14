using FluentAssertions;
using NetArchTest.Rules;
using Payroll.Application;
using Payroll.Application.Interfaces;
using Xunit;

namespace Payroll.Api.Tests.Architecture;

public sealed class ArchitectureTests
{
    private const string EngineNs         = "Payroll.Engine";
    private const string DomainNs         = "Payroll.Domain";
    private const string ApplicationNs    = "Payroll.Application";
    private const string InfrastructureNs = "Payroll.Infrastructure";
    private const string ApiNs            = "Payroll.Api";

    [Fact]
    public void Engine_HasZeroDependencies_OnAnyLayer()
    {
        Types.InAssembly(typeof(Payroll.Engine.PayrollEngine).Assembly)
             .ShouldNot()
             .HaveDependencyOnAny(
                 DomainNs, ApplicationNs, InfrastructureNs, ApiNs,
                 "Microsoft.EntityFrameworkCore",
                 "Microsoft.Extensions.DependencyInjection",
                 "StackExchange.Redis",
                 "Hangfire")
             .GetResult()
             .IsSuccessful
             .Should().BeTrue("Payroll.Engine must be a pure library with zero framework deps");
    }

    [Fact]
    public void Domain_DoesNotDependOn_ApplicationInfrastructureOrApi()
    {
        Types.InAssembly(typeof(Payroll.Domain.Interfaces.ITenantContext).Assembly)
             .ShouldNot()
             .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs,
                 "Microsoft.EntityFrameworkCore",
                 "StackExchange.Redis")
             .GetResult()
             .IsSuccessful
             .Should().BeTrue("Domain must not depend on Application, Infrastructure, or Api");
    }

    [Fact]
    public void Application_DoesNotDependOn_InfrastructureOrApi()
    {
        Types.InAssembly(typeof(Payroll.Application.ApplicationAssemblyMarker).Assembly)
             .ShouldNot()
             .HaveDependencyOnAny(InfrastructureNs, ApiNs,
                 "Microsoft.EntityFrameworkCore",
                 "StackExchange.Redis",
                 "Hangfire")
             .GetResult()
             .IsSuccessful
             .Should().BeTrue("Application must not depend on Infrastructure or Api");
    }

    [Fact]
    public void IAllowWithoutTenant_Implementors_MustBeInPlatformNamespace()
    {
        // Enforces the convention: only SuperAdmin-level commands/queries may bypass tenant validation.
        // They must live in Payroll.Application.Commands.Platform or Payroll.Application.Queries.Platform.
        IEnumerable<Type> types = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
             .That()
             .ImplementInterface(typeof(IAllowWithoutTenant))
             .GetTypes();

        bool allInPlatform = types.All(t =>
            t.Namespace?.StartsWith("Payroll.Application.Commands.Platform") == true ||
            t.Namespace?.StartsWith("Payroll.Application.Queries.Platform") == true);

        allInPlatform.Should().BeTrue(
            "IAllowWithoutTenant types must reside in Payroll.Application.Commands.Platform or Payroll.Application.Queries.Platform");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOn_Api()
    {
        Types.InAssembly(typeof(Payroll.Infrastructure.Persistence.PayrollDbContext).Assembly)
             .ShouldNot()
             .HaveDependencyOn(ApiNs)
             .GetResult()
             .IsSuccessful
             .Should().BeTrue("Infrastructure must not depend on Api");
    }
}
