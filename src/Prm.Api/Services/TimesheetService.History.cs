using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    public async Task<MyTimesheetsResponse> GetMyTimesheets(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdOrThrow(userId, cancellationToken);
        var summaries = await BuildTimesheetHistorySummaries(employee.Id, cancellationToken);
        return ToMyTimesheetsResponse(summaries);
    }

    public async Task<TimesheetWeekDetailResponse> GetMyTimesheetDetail(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeByUserIdOrThrow(userId, cancellationToken);
        return await GetTimesheetDetailForEmployee(employee.Id, weekStart, cancellationToken);
    }

    private async Task<Dictionary<DateOnly, TimesheetWeekSummary>> BuildTimesheetHistorySummaries(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var historyStart = TimesheetWeekHelper.GetHistoryStart(
            lastCompletedWeekStart,
            TimesheetConstants.HistoryWeeksCount);

        var summaries = await LoadSubmittedSummaries(employeeId, historyStart, cancellationToken);
        await AppendMissedWeekSummaries(
            summaries,
            employeeId,
            lastCompletedWeekStart,
            historyStart,
            cancellationToken);

        return summaries;
    }

    private async Task<Dictionary<DateOnly, TimesheetWeekSummary>> LoadSubmittedSummaries(
        int employeeId,
        DateOnly historyStart,
        CancellationToken cancellationToken)
    {
        var submitted = await _timesheetRepository.GetByEmployeeId(employeeId, cancellationToken);
        return submitted
            .Where(x => x.WeekStart >= historyStart)
            .Select(x => new TimesheetWeekSummary
            {
                WeekStart = x.WeekStart,
                TotalHours = x.TotalHours,
                Status = x.Status,
            })
            .ToDictionary(x => x.WeekStart);
    }

    private async Task AppendMissedWeekSummaries(
        Dictionary<DateOnly, TimesheetWeekSummary> summaries,
        int employeeId,
        DateOnly lastCompletedWeekStart,
        DateOnly historyStart,
        CancellationToken cancellationToken)
    {
        for (var weekStart = lastCompletedWeekStart;
             weekStart >= historyStart;
             weekStart = weekStart.AddDays(-7))
        {
            if (summaries.ContainsKey(weekStart))
            {
                continue;
            }

            var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
            var allocations = await GetAllocationsOverlappingWeek(employeeId, weekStart, weekEnd, cancellationToken);
            if (allocations.Count == 0)
            {
                continue;
            }

            summaries[weekStart] = new TimesheetWeekSummary
            {
                WeekStart = weekStart,
                TotalHours = 0,
                Status = TimesheetConstants.StatusMissed,
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
                })
                .ToList(),
        };
    }

    private async Task<TimesheetWeekDetailResponse> GetTimesheetDetailForEmployee(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var timesheet = await _timesheetRepository.GetByEmployeeAndWeek(
            employeeId,
            normalizedWeekStart,
            cancellationToken);

        if (timesheet is not null)
        {
            return new TimesheetWeekDetailResponse
            {
                WeekStart = normalizedWeekStart,
                Status = timesheet.Status,
                TotalHours = timesheet.TotalHours,
                Entries = MapEntryDetails(timesheet.Entries),
            };
        }

        return await BuildMissedTimesheetWeekDetail(employeeId, normalizedWeekStart, cancellationToken);
    }

    private async Task<TimesheetWeekDetailResponse> BuildMissedTimesheetWeekDetail(
        int employeeId,
        DateOnly normalizedWeekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var allocations = await GetAllocationsOverlappingWeek(employeeId, normalizedWeekStart, weekEnd, cancellationToken);
        if (allocations.Count == 0)
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);
        }

        return new TimesheetWeekDetailResponse
        {
            WeekStart = normalizedWeekStart,
            Status = TimesheetConstants.StatusMissed,
            TotalHours = 0,
            Entries = [],
        };
    }
}
