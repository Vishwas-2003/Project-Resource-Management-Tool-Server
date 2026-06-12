using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories.Interfaces;

public interface ITimesheetRepository : ICrudBaseRepository<Timesheet, int>
{
    Task<IReadOnlyList<string>> GetRecentActivityTagNamesForUser(
        int userId,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default);

    Task<int> GetHoursWorkedForUserOnProjectInWeek(
        int userId,
        int projectId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTag>> GetAllActivityTags(CancellationToken cancellationToken = default);

    Task<ActivityTag?> GetActivityTagById(int activityTagId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTag>> GetActivityTagsByIds(
        IReadOnlyCollection<int> activityTagIds,
        CancellationToken cancellationToken = default);

    Task<ActivityTag> FindOrCreateActivityTagByName(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<bool> IsSubmittedForUserWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<Timesheet> EnsureBlockedTimesheetAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Timesheet>> GetByUserId(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Timesheet?> GetByUserAndWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamTimesheetEntryRow>> GetEntriesForTeamByManagerAndWeek(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
}
