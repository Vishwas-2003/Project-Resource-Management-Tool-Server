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
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetTeamEmployeeOrThrow(managerUserId, employeeId, cancellationToken);
        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var timesheet = await _timesheetRepository.GetByEmployeeAndWeek(
            employee.Id,
            normalizedWeekStart,
            cancellationToken);

        if (timesheet is null)
        {
            return await BuildMissedEmployeeTimesheetDetail(employee, normalizedWeekStart, cancellationToken);
        }

        return MapSubmittedEmployeeTimesheetDetail(employee, normalizedWeekStart, timesheet);
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
            EmployeeId = x.EmployeeId,
            EmployeeName = x.EmployeeName,
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
        var submittedEmployeeIds = rows
            .Select(x => x.EmployeeId)
            .ToHashSet();

        var teamEmployees = await _employeeRepository.GetEmployeesByManagerUserId(managerUserId, cancellationToken);
        foreach (var employee in teamEmployees)
        {
            if (submittedEmployeeIds.Contains(employee.Id))
            {
                continue;
            }

            var allocations = await _allocationRepository.GetOverlappingForEmployee(
                new EmployeeAllocationPeriodQuery
                {
                    EmployeeId = employee.Id,
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
                EmployeeId = employee.Id,
                EmployeeName = employee.User.FullName,
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

    private async Task<Employee> GetTeamEmployeeOrThrow(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetEmployeeDetailById(employeeId, cancellationToken);
        if (employee is null || employee.ManagerUserId != managerUserId)
        {
            throw new UnauthorizedAccessException(AppConstants.Timesheets.EmployeeNotOnTeam);
        }

        return employee;
    }

    private async Task<EmployeeTimesheetDetailResponse> BuildMissedEmployeeTimesheetDetail(
        Employee employee,
        DateOnly normalizedWeekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(normalizedWeekStart);
        var allocations = await _allocationRepository.GetOverlappingForEmployee(
            new EmployeeAllocationPeriodQuery
            {
                EmployeeId = employee.Id,
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
            EmployeeId = employee.Id,
            EmployeeName = employee.User.FullName,
            WeekStart = normalizedWeekStart,
            Status = TimesheetConstants.StatusMissed,
            TotalHours = 0,
            Entries = [],
        };
    }

    private static EmployeeTimesheetDetailResponse MapSubmittedEmployeeTimesheetDetail(
        Employee employee,
        DateOnly normalizedWeekStart,
        Timesheet timesheet) =>
        new()
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.User.FullName,
            WeekStart = normalizedWeekStart,
            Status = timesheet.Status,
            TotalHours = timesheet.TotalHours,
            Entries = MapEntryDetails(timesheet.Entries),
        };
}
