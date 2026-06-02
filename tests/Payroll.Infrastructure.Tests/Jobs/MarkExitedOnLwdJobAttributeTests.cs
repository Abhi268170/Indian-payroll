using FluentAssertions;
using Hangfire;
using Payroll.Infrastructure.Jobs;
using Xunit;

namespace Payroll.Infrastructure.Tests.Jobs;

/// <summary>
/// WI-12: the daily exit sweep is idempotent, so transient failures must be
/// retried rather than permanently dropped (which would leave a past-LWD
/// employee showing Active until the next day's run). This guards the retry
/// setting against regression to Attempts = 0.
/// </summary>
public class MarkExitedOnLwdJobAttributeTests
{
    [Fact]
    public void MarkExitedOnLwdJob_HasAutomaticRetry_WithThreeAttempts()
    {
        AutomaticRetryAttribute? retry = (AutomaticRetryAttribute?)Attribute.GetCustomAttribute(
            typeof(MarkExitedOnLwdJob), typeof(AutomaticRetryAttribute));

        retry.Should().NotBeNull("the idempotent daily sweep must retry transient failures");
        retry!.Attempts.Should().Be(3,
            "WI-12: retry attempts must be 3, not 0 — a transient DB failure must not "
            + "permanently skip the status flip");
    }
}
