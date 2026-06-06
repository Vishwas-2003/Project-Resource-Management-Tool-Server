using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories.Interfaces;

public interface ITimesheetRepository : ICrudBaseRepository<Timesheet, int>
{
    Task<IReadOnlyList<string>> GetRecentActivityTagNamesForEmployee(
        int employeeId,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default);

    Task<int> GetHoursWorkedForEmployeeOnProjectInWeek(
        int employeeId,
        int projectId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTag>> GetAllActivityTags(CancellationToken cancellationToken = default);

    Task<ActivityTag?> GetActivityTagById(int activityTagId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTag>> GetActivityTagsByIds(
        IReadOnlyCollection<int> activityTagIds,
        CancellationToken cancellationToken = default);

    Task<ActivityTag> FindOrCreateActivityTagByName(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsForEmployeeWeek(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Timesheet>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByEmployeeAndWeek(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamTimesheetEntryRow>> GetEntriesForTeamByManagerAndWeek(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}
