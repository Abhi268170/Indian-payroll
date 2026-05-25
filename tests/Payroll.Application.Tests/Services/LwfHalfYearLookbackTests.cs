using FluentAssertions;
using Payroll.Application.Services;
using Xunit;

namespace Payroll.Application.Tests.Services;

// Locks down the 6-branch wrap-around logic for FnF LWF duplicate-protection.
// Without these tests the audit finding is "fixed in code but unprotected" —
// a future refactor could revert to the placeholder and tests stay green.
public class LwfHalfYearLookbackTests
{
    // ── H1 (Apr–Sep) ──────────────────────────────────────────────────────────

    [Fact]
    public void April_NoEarlierMonthsInH1_ReturnsEmpty()
    {
        LwfHalfYearLookback.GetRanges(2025, 4).Should().BeEmpty();
    }

    [Theory]
    [InlineData(5, 4, 4)]
    [InlineData(6, 4, 5)]
    [InlineData(9, 4, 8)]
    public void H1_MonthN_LooksAt_AprToN_minus_1_SameYear(int month, int first, int last)
    {
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2025, month);
        ranges.Should().BeEquivalentTo([(2025, first, last)]);
    }

    // ── H2 first half (Oct–Dec) ───────────────────────────────────────────────

    [Fact]
    public void October_NoEarlierMonthsInH2_ReturnsEmpty()
    {
        LwfHalfYearLookback.GetRanges(2025, 10).Should().BeEmpty();
    }

    [Theory]
    [InlineData(11, 10, 10)]
    [InlineData(12, 10, 11)]
    public void H2FirstHalf_MonthN_LooksAt_OctToN_minus_1_SameYear(int month, int first, int last)
    {
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2025, month);
        ranges.Should().BeEquivalentTo([(2025, first, last)]);
    }

    // ── H2 second half (Jan–Mar) — wraps to prior calendar year ──────────────

    [Fact]
    public void January_OnlyChecksPriorYearOctToDec()
    {
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2026, 1);
        ranges.Should().BeEquivalentTo([(2025, 10, 12)]);
    }

    [Fact]
    public void February_ChecksPriorYearH2Tail_AND_CurrentJanuary()
    {
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2026, 2);
        ranges.Should().BeEquivalentTo([(2025, 10, 12), (2026, 1, 1)]);
    }

    [Fact]
    public void March_ChecksPriorYearH2Tail_AND_CurrentJanFeb()
    {
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2026, 3);
        ranges.Should().BeEquivalentTo([(2025, 10, 12), (2026, 1, 2)]);
    }

    // ── Cross-year semantics ─────────────────────────────────────────────────

    [Fact]
    public void FebruaryWrap_PriorYearTail_UsesGivenYearMinusOne()
    {
        // FY 2024-25 March: the H2 window started in Oct 2024 of *that same* calendar year.
        IReadOnlyList<(int Year, int First, int Last)> ranges = LwfHalfYearLookback.GetRanges(2025, 3);
        ranges.Should().BeEquivalentTo([(2024, 10, 12), (2025, 1, 2)]);
    }
}
