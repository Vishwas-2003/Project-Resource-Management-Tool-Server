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
            cancellationToken);

        if (timesheet is null)
        {
            return await BuildMissedResourceTimesheetDetail(user, normalizedWeekStart, cancellationToken);
        }

        return MapSubmittedResourceTimesheetDetail(user, normalizedWeekStart, timesheet);
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
            ResourceUserId = x.UserId,
            ResourceName = x.UserName,
            ProjectName = x.ProjectName,
            HoursWorked = x.Hours,
            Status = x.Status,
            Access = TimesheetConstants.AccessAllowed,
        }).ToList();
    }

    private async Task AppendMissedTeamRows(
        List<TeamTimesheetRow> rows,
        int managerUserId,
        DateOnly normalizedWeekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken)
    {
        var teamUsers = await _userRepository.GetResourceUsersByManagerUserId(managerUserId, cancellationToken);
        foreach (var user in teamUsers)
        {
            if (await _timesheetRepository.IsSubmittedForUserWeek(user.Id, normalizedWeekStart, cancellationToken)
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

            var timesheet = await _timesheetRepository.GetByUserAndWeek(
                user.Id,
                normalizedWeekStart,
                cancellationToken);

            rows.Add(new TeamTimesheetRow
            {
                ResourceUserId = user.Id,
                ResourceName = user.FullName,
                ProjectName = allocations[0].Project.Name,
                HoursWorked = 0,
                Status = timesheet?.Status ?? TimesheetConstants.StatusMissed,
                Access = timesheet?.Access ?? TimesheetConstants.AccessAllowed,
            });
        }
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

    private async Task<ResourceTimesheetDetailResponse> BuildMissedResourceTimesheetDetail(
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

        return new ResourceTimesheetDetailResponse
        {
            ResourceUserId = user.Id,
            ResourceName = user.FullName,
            WeekStart = normalizedWeekStart,
            Status = TimesheetConstants.StatusMissed,
            TotalHours = 0,
            Access = TimesheetConstants.AccessAllowed,
            Entries = [],
        };
    }

    private static ResourceTimesheetDetailResponse MapSubmittedResourceTimesheetDetail(
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
