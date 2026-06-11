using Prm.Common.Constants;
using Prm.Data.Entities;

namespace Prm.Api.Services;

internal static class UserAvailabilityHelper
{
    public static DateOnly GetAvailabilityStartDate(DateTime createdAtUtc) =>
        DateOnly.FromDateTime(createdAtUtc);

    public static DateOnly GetFirstEligibleWeekStart(DateTime createdAtUtc) =>
        TimesheetWeekHelper.GetWeekStart(GetAvailabilityStartDate(createdAtUtc));

    public static bool IsWeekEligibleForUser(User user, DateOnly weekStart) =>
        weekStart >= GetFirstEligibleWeekStart(user.CreatedAtUtc);

    public static void EnsureWeekEligibleForUser(User user, DateOnly weekStart)
    {
        if (!IsWeekEligibleForUser(user, weekStart))
        {
            throw new ArgumentException(AppConstants.Timesheets.WeekBeforeResourceCreated);
        }
    }

    public static void EnsureAllocationDatesEligibleForUser(User user, DateOnly fromDate, DateOnly toDate)
    {
        var availabilityStart = GetAvailabilityStartDate(user.CreatedAtUtc);
        if (fromDate < availabilityStart || toDate < availabilityStart)
        {
            throw new ArgumentException(AppConstants.Allocations.AllocationDatesBeforeResourceCreated);
        }
    }
}
