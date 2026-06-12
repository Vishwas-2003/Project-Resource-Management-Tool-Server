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

        return await LoadTimesheetSummaries(user.Id, eligibleHistoryStart, cancellationToken);
    }

    private static DateOnly MaxDate(DateOnly left, DateOnly right) =>
        left > right ? left : right;

    private async Task<Dictionary<DateOnly, TimesheetWeekSummary>> LoadTimesheetSummaries(
        int userId,
        DateOnly historyStart,
        CancellationToken cancellationToken)
    {
        var timesheets = await _timesheetRepository.GetByUserId(userId, cancellationToken);
        return timesheets
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
            cancellationToken)
            ?? throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);

        return new TimesheetWeekDetailResponse
        {
            WeekStart = normalizedWeekStart,
            Status = timesheet.Status,
            TotalHours = timesheet.TotalHours,
            Access = timesheet.Access,
            Entries = MapEntryDetails(timesheet.Entries),
        };
    }
}
