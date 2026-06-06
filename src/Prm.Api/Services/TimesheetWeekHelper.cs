namespace Prm.Api.Services;

internal static class TimesheetWeekHelper
{
    public static DateOnly GetWeekStart(DateOnly date)
    {
        var daysFromMonday = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.AddDays(-daysFromMonday);
    }

    public static DateOnly GetWeekEnd(DateOnly weekStart) => weekStart.AddDays(6);

    public static DateOnly GetLastCompletedWeekStart(DateOnly today)
    {
        var currentWeekStart = GetWeekStart(today);
        var candidate = currentWeekStart.AddDays(-7);
        while (GetWeekEnd(candidate) >= today)
        {
            candidate = candidate.AddDays(-7);
        }

        return candidate;
    }

    public static DateOnly GetHistoryStart(DateOnly lastCompletedWeekStart, int historyWeeksCount) =>
        lastCompletedWeekStart.AddDays(-7 * (historyWeeksCount - 1));

    public static int ComputeExpectedHours(int utilizationPercent, int maxWeeklyHours) =>
        utilizationPercent * maxWeeklyHours / 100;
}
