using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    public async Task<TeamTimesheetsResponse> GetTeamTimesheets(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var timesheets = await _timesheetRepository.GetTimesheetsForTeamByManagerAndWeek(
            managerUserId,
            normalizedWeekStart,
            cancellationToken);
        var rows = await BuildTeamTimesheetRows(timesheets, normalizedWeekStart, weekEnd, cancellationToken);

        return new TeamTimesheetsResponse
        {
            WeekStart = normalizedWeekStart,
            Rows = AssignTeamRowNumbers(rows),
        };
    }

    public async Task<ResourceTimesheetDetailResponse> GetResourceTimesheetDetail(
        int managerUserId,
        int resourceUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var user = await GetTeamResourceOrThrow(managerUserId, resourceUserId, cancellationToken);
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var timesheet = await _timesheetRepository.GetByUserAndWeek(
            user.Id,
            normalizedWeekStart,
            cancellationToken)
            ?? throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);

        return MapResourceTimesheetDetail(user, normalizedWeekStart, timesheet);
    }

    private async Task<List<TeamTimesheetRow>> BuildTeamTimesheetRows(
        IReadOnlyList<Timesheet> timesheets,
        DateOnly normalizedWeekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken)
    {
        var rows = new List<TeamTimesheetRow>();

        foreach (var timesheet in timesheets)
        {
            if (timesheet.Status == TimesheetConstants.StatusSubmitted && timesheet.Entries.Count > 0)
            {
                rows.AddRange(timesheet.Entries
                    .OrderBy(entry => entry.Project.Name)
                    .Select(entry => new TeamTimesheetRow
                    {
                        ResourceUserId = timesheet.UserId,
                        ResourceName = timesheet.User.FullName,
                        ProjectName = entry.Project.Name,
                        HoursWorked = entry.HoursWorked,
                        Status = timesheet.Status,
                        Access = timesheet.Access,
                    }));
                continue;
            }

            if (timesheet.Status != TimesheetConstants.StatusMissed)
            {
                continue;
            }

            var allocations = await _allocationRepository.GetOverlappingForUser(
                new UserAllocationPeriodQuery
                {
                    UserId = timesheet.UserId,
                    FromDate = normalizedWeekStart,
                    ToDate = weekEnd,
                },
                cancellationToken);

            rows.Add(new TeamTimesheetRow
            {
                ResourceUserId = timesheet.UserId,
                ResourceName = timesheet.User.FullName,
                ProjectName = allocations.FirstOrDefault()?.Project?.Name ?? "-",
                HoursWorked = 0,
                Status = timesheet.Status,
                Access = timesheet.Access,
            });
        }

        return rows;
    }

    private static List<TeamTimesheetRow> AssignTeamRowNumbers(List<TeamTimesheetRow> rows) =>
        rows
            .OrderBy(x => x.ResourceName)
            .ThenBy(x => x.ProjectName)
            .Select((row, index) =>
            {
                row.RowNumber = index + 1;
                return row;
            })
            .ToList();

    private async Task<User> GetTeamResourceOrThrow(
        int managerUserId,
        int userId,
        CancellationToken cancellationToken)
    {
        var teamUsers = await _userRepository.GetResourceUsersByManagerUserId(managerUserId, cancellationToken);
        var user = teamUsers.FirstOrDefault(x => x.Id == userId);
        if (user is null)
        {
            throw new UnauthorizedAccessException(AppConstants.Timesheets.ResourceNotOnTeam);
        }

        return user;
    }

    private static ResourceTimesheetDetailResponse MapResourceTimesheetDetail(
        User user,
        DateOnly normalizedWeekStart,
        Timesheet timesheet) =>
        new()
        {
            ResourceUserId = user.Id,
            ResourceName = user.FullName,
            WeekStart = normalizedWeekStart,
            Status = timesheet.Status,
            TotalHours = timesheet.TotalHours,
            Access = timesheet.Access,
            Entries = MapEntryDetails(timesheet.Entries),
        };
}
