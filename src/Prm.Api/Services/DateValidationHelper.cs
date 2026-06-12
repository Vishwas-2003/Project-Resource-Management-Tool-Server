namespace Prm.Api.Services;

internal static class DateValidationHelper
{
    public static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    public static void EnsureNotBeforeToday(DateOnly date, string message)
    {
        if (date < TodayUtc)
        {
            throw new ArgumentException(message);
        }
    }
}
