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
        var rows = await BuildSubmittedTeamRows(managerUserId, normalizedWeekStart, cancellationToken);
        await AppendMissedTeamRows(rows, managerUserId, normalizedWeekStart, weekEnd, cancellationToken);

        return new TeamTimesheetsResponse
        {
            WeekStart = normalizedWeekStart,
            Rows = AssignTeamRowNumbers(rows),
        };
    }

    public async Task<EmployeeTimesheetDetailResponse> GetEmployeeTimesheetDetail(
        int managerUserId,
        int employeeUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var user = await GetTeamEmployeeOrThrow(managerUserId, employeeUserId, cancellationToken);
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var timesheet = await _timesheetRepository.GetByUserAndWeek(
            user.Id,
            normalizedWeekStart,
            cancellationToken);

        if (timesheet is null)
        {
            return await BuildMissedEmployeeTimesheetDetail(user, normalizedWeekStart, cancellationToken);
        }

        return MapSubmittedEmployeeTimesheetDetail(user, normalizedWeekStart, timesheet);
    }

    private async Task<List<TeamTimesheetRow>> BuildSubmittedTeamRows(
        int managerUserId,
        DateOnly normalizedWeekStart,
        CancellationToken cancellationToken)
    {
        var submittedRows = await _timesheetRepository.GetEntriesForTeamByManagerAndWeek(
            managerUserId,
            normalizedWeekStart,
            cancellationToken);

        return submittedRows.Select(x => new TeamTimesheetRow
        {
            EmployeeUserId = x.UserId,
            EmployeeName = x.UserName,
            ProjectName = x.ProjectName,
            HoursWorked = x.Hours,
            Status = x.Status,
        }).ToList();
    }

    private async Task AppendMissedTeamRows(
        List<TeamTimesheetRow> rows,
        int managerUserId,
        DateOnly normalizedWeekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken)
    {
        var submittedEmployeeUserIds = rows
            .Select(x => x.EmployeeUserId)
            .ToHashSet();

        var teamUsers = await _userRepository.GetEmployeeUsersByManagerUserId(managerUserId, cancellationToken);
        foreach (var user in teamUsers)
        {
            if (submittedEmployeeUserIds.Contains(user.Id)
                || !UserAvailabilityHelper.IsWeekEligibleForUser(user, normalizedWeekStart))
            {
                continue;
            }

            var allocations = await _allocationRepository.GetOverlappingForUser(
                new UserAllocationPeriodQuery
                {
                    UserId = user.Id,
                    FromDate = normalizedWeekStart,
                    ToDate = weekEnd,
                },
                cancellationToken);

            if (allocations.Count == 0)
            {
                continue;
            }

            rows.Add(new TeamTimesheetRow
            {
                EmployeeUserId = user.Id,
                EmployeeName = user.FullName,
                ProjectName = allocations[0].Project.Name,
                HoursWorked = 0,
                Status = TimesheetConstants.StatusMissed,
            });
        }
    }

    private static List<TeamTimesheetRow> AssignTeamRowNumbers(List<TeamTimesheetRow> rows) =>
        rows
            .OrderBy(x => x.EmployeeName)
            .ThenBy(x => x.ProjectName)
            .Select((row, index) =>
            {
                row.RowNumber = index + 1;
                return row;
            })
            .ToList();

    private async Task<User> GetTeamEmployeeOrThrow(
        int managerUserId,
        int userId,
        CancellationToken cancellationToken)
    {
        var teamUsers = await _userRepository.GetEmployeeUsersByManagerUserId(managerUserId, cancellationToken);
        var user = teamUsers.FirstOrDefault(x => x.Id == userId);
        if (user is null)
        {
            throw new UnauthorizedAccessException(AppConstants.Timesheets.EmployeeNotOnTeam);
        }

        return user;
    }

    private async Task<EmployeeTimesheetDetailResponse> BuildMissedEmployeeTimesheetDetail(
        User user,
        DateOnly normalizedWeekStart,
        CancellationToken cancellationToken)
    {
        if (!UserAvailabilityHelper.IsWeekEligibleForUser(user, normalizedWeekStart))
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);
        }

        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var allocations = await _allocationRepository.GetOverlappingForUser(
            new UserAllocationPeriodQuery
            {
                UserId = user.Id,
                FromDate = normalizedWeekStart,
                ToDate = weekEnd,
            },
            cancellationToken);

        if (allocations.Count == 0)
        {
            throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);
        }

        return new EmployeeTimesheetDetailResponse
        {
            EmployeeUserId = user.Id,
            EmployeeName = user.FullName,
            WeekStart = normalizedWeekStart,
            Status = TimesheetConstants.StatusMissed,
            TotalHours = 0,
            Entries = [],
        };
    }

    private static EmployeeTimesheetDetailResponse MapSubmittedEmployeeTimesheetDetail(
        User user,
        DateOnly normalizedWeekStart,
        Timesheet timesheet) =>
        new()
        {
            EmployeeUserId = user.Id,
            EmployeeName = user.FullName,
            WeekStart = normalizedWeekStart,
            Status = timesheet.Status,
            TotalHours = timesheet.TotalHours,
            Entries = MapEntryDetails(timesheet.Entries),
        };
}
