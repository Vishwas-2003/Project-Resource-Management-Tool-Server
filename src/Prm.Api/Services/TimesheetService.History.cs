using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    public async Task<MyTimesheetsResponse> GetMyTimesheets(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(userId, cancellationToken);
        var summaries = await BuildTimesheetHistorySummaries(user, cancellationToken);
        return ToMyTimesheetsResponse(summaries);
    }

    public async Task<TimesheetWeekDetailResponse> GetMyTimesheetDetail(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(userId, cancellationToken);
        return await GetTimesheetDetailForUser(user, weekStart, cancellationToken);
    }

    private async Task<Dictionary<DateOnly, TimesheetWeekSummary>> BuildTimesheetHistorySummaries(
        User user,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var historyStart = TimesheetWeekHelper.GetHistoryStart(
            lastCompletedWeekStart,
            TimesheetConstants.HistoryWeeksCount);
        var eligibleHistoryStart = MaxDate(
            historyStart,
            UserAvailabilityHelper.GetFirstEligibleWeekStart(user.CreatedAtUtc));

        var summaries = await LoadSubmittedSummaries(user.Id, eligibleHistoryStart, cancellationToken);
        await AppendMissedWeekSummaries(
            summaries,
            user,
            lastCompletedWeekStart,
            eligibleHistoryStart,
            cancellationToken);

        return summaries;
    }

    private static DateOnly MaxDate(DateOnly left, DateOnly right) =>
        left > right ? left : right;

    private async Task<Dictionary<DateOnly, TimesheetWeekSummary>> LoadSubmittedSummaries(
        int userId,
        DateOnly historyStart,
        CancellationToken cancellationToken)
    {
        var submitted = await _timesheetRepository.GetByUserId(userId, cancellationToken);
        return submitted
            .Where(x => x.WeekStart >= historyStart)
            .Select(x => new TimesheetWeekSummary
            {
                WeekStart = x.WeekStart,
                TotalHours = x.TotalHours,
                Status = x.Status,
                Access = x.Access,
            })
            .ToDictionary(x => x.WeekStart);
    }

    private async Task AppendMissedWeekSummaries(
        Dictionary<DateOnly, TimesheetWeekSummary> summaries,
        User user,
        DateOnly lastCompletedWeekStart,
        DateOnly historyStart,
        CancellationToken cancellationToken)
    {
        for (var weekStart = lastCompletedWeekStart;
             weekStart >= historyStart;
             weekStart = weekStart.AddDays(-7))
        {
            if (summaries.ContainsKey(weekStart)
                || !UserAvailabilityHelper.IsWeekEligibleForUser(user, weekStart))
            {
                continue;
            }

            var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
            var allocations = await GetAllocationsOverlappingWeek(user.Id, weekStart, weekEnd, cancellationToken);
            if (allocations.Count == 0)
            {
                continue;
            }

            summaries[weekStart] = new TimesheetWeekSummary
            {
                WeekStart = weekStart,
                TotalHours = 0,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessAllowed,
            };
        }
    }

    private static MyTimesheetsResponse ToMyTimesheetsResponse(
        Dictionary<DateOnly, TimesheetWeekSummary> summaries)
    {
        var ordered = summaries.Values
            .OrderByDescending(x => x.WeekStart)
            .ToList();

        return new MyTimesheetsResponse
        {
            Timesheets = ordered
                .Select((summary, index) => new MyTimesheetRow
                {
                    RowNumber = index + 1,
                    WeekStart = summary.WeekStart,
                    TotalHours = summary.TotalHours,
                    Status = summary.Status,
                    Access = summary.Access,
                })
                .ToList(),
        };
    }

    private async Task<TimesheetWeekDetailResponse> GetTimesheetDetailForUser(
        User user,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        if (!UserAvailabilityHelper.IsWeekEligibleForUser(user, normalizedWeekStart))
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);
        }

        var timesheet = await _timesheetRepository.GetByUserAndWeek(
            user.Id,
            normalizedWeekStart,
            cancellationToken);

        if (timesheet is not null)
        {
            return new TimesheetWeekDetailResponse
            {
                WeekStart = normalizedWeekStart,
                Status = timesheet.Status,
                TotalHours = timesheet.TotalHours,
                Access = timesheet.Access,
                Entries = MapEntryDetails(timesheet.Entries),
            };
        }

        return await BuildMissedTimesheetWeekDetail(user, normalizedWeekStart, cancellationToken);
    }

    private async Task<TimesheetWeekDetailResponse> BuildMissedTimesheetWeekDetail(
        User user,
        DateOnly normalizedWeekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var allocations = await GetAllocationsOverlappingWeek(user.Id, normalizedWeekStart, weekEnd, cancellationToken);
        if (allocations.Count == 0)
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);
        }

        return new TimesheetWeekDetailResponse
        {
            WeekStart = normalizedWeekStart,
            Status = TimesheetConstants.StatusMissed,
            TotalHours = 0,
            Access = TimesheetConstants.AccessAllowed,
            Entries = [],
        };
    }
}
