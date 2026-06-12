using Prm.Common.Constants;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class SchedulerService
{
    private async Task RecordMissedTimesheets(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var historyStart = TimesheetWeekHelper.GetHistoryStart(
            lastCompletedWeekStart,
            TimesheetConstants.HistoryWeeksCount);

        var users = await _userRepository.GetResourceUsers(
            new ResourceFilter { IncludeInactive = false },
            cancellationToken);

        var createdCount = 0;

        foreach (var user in users)
        {
            var eligibleHistoryStart = MaxDate(
                historyStart,
                UserAvailabilityHelper.GetFirstEligibleWeekStart(user.CreatedAtUtc));

            for (var weekStart = lastCompletedWeekStart;
                 weekStart >= eligibleHistoryStart;
                 weekStart = weekStart.AddDays(-7))
            {
                if (!UserAvailabilityHelper.IsWeekEligibleForUser(user, weekStart))
                {
                    continue;
                }

                if (await _timesheetRepository.IsSubmittedForUserWeek(user.Id, weekStart, cancellationToken))
                {
                    continue;
                }

                var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
                var allocations = await _allocationRepository.GetOverlappingForUser(
                    new UserAllocationPeriodQuery
                    {
                        UserId = user.Id,
                        FromDate = weekStart,
                        ToDate = weekEnd,
                    },
                    cancellationToken);

                if (allocations.Count == 0)
                {
                    continue;
                }

                if (await _timesheetRepository.TryEnsureMissedTimesheetAsync(user.Id, weekStart, cancellationToken))
                {
                    createdCount++;
                }
            }
        }

        if (createdCount > 0)
        {
            await _timesheetRepository.SaveChanges(cancellationToken);
        }

        _logger.LogInformation("Recorded {CreatedCount} missed timesheet entries.", createdCount);
    }

    private static DateOnly MaxDate(DateOnly left, DateOnly right) =>
        left > right ? left : right;
}
