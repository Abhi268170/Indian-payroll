using FluentAssertions;
using Payroll.Domain.Entities;
using Xunit;

namespace Payroll.Application.Tests.DomainEntities;

/// <summary>WI-19: FnF exemption limits live on statutory config.</summary>
public class StatutoryExemptionLimitsTests
{
    [Fact]
    public void Defaults_Are20LAnd25L()
    {
        StatutoryOrgConfig c = StatutoryOrgConfig.CreateDefault(Guid.NewGuid(), Guid.NewGuid());
        c.GratuityExemptionLimit.Should().Be(2_000_000m);
        c.LeaveEncashmentExemptionLimit.Should().Be(2_500_000m);
    }

    [Fact]
    public void ConfigureFnfExemptionLimits_UpdatesBoth()
    {
        StatutoryOrgConfig c = StatutoryOrgConfig.CreateDefault(Guid.NewGuid(), Guid.NewGuid());
        c.ConfigureFnfExemptionLimits(3_000_000m, 4_000_000m, Guid.NewGuid());
        c.GratuityExemptionLimit.Should().Be(3_000_000m);
        c.LeaveEncashmentExemptionLimit.Should().Be(4_000_000m);
    }
}
