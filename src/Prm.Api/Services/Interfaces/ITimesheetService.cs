using Prm.Common.Models.Timesheets;

namespace Prm.Api.Services.Interfaces;

public interface ITimesheetService
{
    Task<ActivityTagsResponse> GetActivityTags(CancellationToken cancellationToken = default);

    Task<MissingTimesheetReminder> GetMissingReminder(
        int userId,
        CancellationToken cancellationToken = default);

    Task<WeekAllocationsResponse> GetWeekAllocations(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<SubmitTimesheetResponse> SubmitTimesheet(
        int userId,
        SubmitTimesheetRequest request,
        CancellationToken cancellationToken = default);

    Task<MyTimesheetsResponse> GetMyTimesheets(
        int userId,
        CancellationToken cancellationToken = default);

    Task<TimesheetWeekDetailResponse> GetMyTimesheetDetail(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<EmployeeAllocationsResponse> GetMyAllocations(
        int userId,
        CancellationToken cancellationToken = default);

    Task<TeamTimesheetsResponse> GetTeamTimesheets(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<EmployeeTimesheetDetailResponse> GetEmployeeTimesheetDetail(
        int managerUserId,
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}
