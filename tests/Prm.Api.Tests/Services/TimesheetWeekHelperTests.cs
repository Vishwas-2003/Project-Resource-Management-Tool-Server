using Prm.Api.Services;

namespace Prm.Api.Tests.Services;

public class TimesheetWeekHelperTests
{
    [Fact]
    public void GetWeekStart_ForMonday_ReturnsSameDate()
    {
        var monday = new DateOnly(2026, 6, 1);

        var weekStart = TimesheetWeekHelper.GetWeekStart(monday);

        Assert.Equal(monday, weekStart);
    }

    [Fact]
    public void GetWeekStart_ForSunday_ReturnsPreviousMonday()
    {
        var sunday = new DateOnly(2026, 6, 7);
        var expectedMonday = new DateOnly(2026, 6, 1);

        var weekStart = TimesheetWeekHelper.GetWeekStart(sunday);

        Assert.Equal(expectedMonday, weekStart);
    }

    [Fact]
    public void GetWeekEnd_ReturnsSixDaysAfterWeekStart()
    {
        var weekStart = new DateOnly(2026, 6, 1);
        var expectedWeekEnd = new DateOnly(2026, 6, 7);

        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);

        Assert.Equal(expectedWeekEnd, weekEnd);
    }

    [Theory]
    [InlineData(100, 40, 40)]
    [InlineData(50, 40, 20)]
    [InlineData(25, 40, 10)]
    public void ComputeExpectedHours_ReturnsUtilizationOfMaxWeeklyHours(
        int utilizationPercent,
        int maxWeeklyHours,
        int expectedHours)
    {
        var result = TimesheetWeekHelper.ComputeExpectedHours(utilizationPercent, maxWeeklyHours);

        Assert.Equal(expectedHours, result);
    }

    [Fact]
    public void GetLastCompletedWeekStart_ReturnsWeekWhoseEndIsBeforeToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentWeekStart = TimesheetWeekHelper.GetWeekStart(today);
        var expected = currentWeekStart.AddDays(-7);

        while (TimesheetWeekHelper.GetWeekEnd(expected) >= today)
        {
            expected = expected.AddDays(-7);
        }

        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);

        Assert.Equal(expected, lastCompletedWeekStart);
        Assert.True(TimesheetWeekHelper.GetWeekEnd(lastCompletedWeekStart) < today);
    }

    [Fact]
    public void GetHistoryStart_ReturnsStartOfHistoryWindow()
    {
        var lastCompletedWeekStart = new DateOnly(2026, 5, 26);
        const int historyWeeksCount = 4;

        var historyStart = TimesheetWeekHelper.GetHistoryStart(lastCompletedWeekStart, historyWeeksCount);

        Assert.Equal(lastCompletedWeekStart.AddDays(-21), historyStart);
    }
}
