using Microsoft.EntityFrameworkCore;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories;

public class TimesheetRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Timesheet, int>(_dbContext), ITimesheetRepository
{
    public async Task<IReadOnlyList<string>> GetRecentActivityTagNamesForUser(
        int userId,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default) =>
        await DbContext.TimesheetActivityTags
            .Where(x =>
                x.TimesheetEntry.Timesheet.UserId == userId
                && x.TimesheetEntry.Timesheet.WeekStart >= sinceDate)
            .Select(x => x.ActivityTag.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<int> GetHoursWorkedForUserOnProjectInWeek(
        int userId,
        int projectId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var hours = await DbContext.TimesheetEntries
            .Where(x =>
                x.Timesheet.UserId == userId
                && x.ProjectId == projectId
                && x.Timesheet.WeekStart == weekStart)
            .SumAsync(x => (int?)x.HoursWorked, cancellationToken);

        return hours ?? 0;
    }

    public async Task<IReadOnlyList<ActivityTag>> GetAllActivityTags(CancellationToken cancellationToken = default) =>
        await DbContext.ActivityTags
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<ActivityTag?> GetActivityTagById(int activityTagId, CancellationToken cancellationToken = default) =>
        DbContext.ActivityTags.FirstOrDefaultAsync(x => x.Id == activityTagId, cancellationToken);

    public async Task<IReadOnlyList<ActivityTag>> GetActivityTagsByIds(
        IReadOnlyCollection<int> activityTagIds,
        CancellationToken cancellationToken = default)
    {
        if (activityTagIds.Count == 0)
        {
            return [];
        }

        return await DbContext.ActivityTags
            .Where(x => activityTagIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ActivityTag> FindOrCreateActivityTagByName(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var existing = await DbContext.ActivityTags
            .FirstOrDefaultAsync(x => x.Name == normalizedName, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var tag = new ActivityTag { Name = normalizedName };
        await DbContext.ActivityTags.AddAsync(tag, cancellationToken);
        return tag;
    }

    public Task<bool> ExistsForUserWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.UserId == userId && x.WeekStart == weekStart, cancellationToken);

    public Task<bool> IsSubmittedForUserWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(
            x => x.UserId == userId
                && x.WeekStart == weekStart
                && x.Status == TimesheetConstants.StatusSubmitted,
            cancellationToken);

    public async Task<bool> TryEnsureMissedTimesheetAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserAndWeek(userId, weekStart, cancellationToken);
        if (existing is not null)
        {
            return false;
        }

        await Add(
            new Timesheet
            {
                UserId = userId,
                WeekStart = weekStart,
                TotalHours = 0,
                Status = TimesheetConstants.StatusMissed,
                Access = TimesheetConstants.AccessAllowed,
            },
            cancellationToken);

        return true;
    }

    public async Task<Timesheet> EnsureBlockedTimesheetAsync(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserAndWeek(userId, weekStart, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == TimesheetConstants.StatusSubmitted)
            {
                return existing;
            }

            if (existing.Access == TimesheetConstants.AccessBlocked)
            {
                return existing;
            }

            existing.Access = TimesheetConstants.AccessBlocked;
            existing.Status = TimesheetConstants.StatusMissed;
            existing.TotalHours = 0;
            Update(existing);
            return existing;
        }

        var timesheet = new Timesheet
        {
            UserId = userId,
            WeekStart = weekStart,
            TotalHours = 0,
            Status = TimesheetConstants.StatusMissed,
            Access = TimesheetConstants.AccessBlocked,
        };

        await Add(timesheet, cancellationToken);
        return timesheet;
    }

    public async Task<IReadOnlyList<Timesheet>> GetByUserId(
        int userId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.WeekStart)
            .ToListAsync(cancellationToken);

    public Task<Timesheet?> GetByUserAndWeek(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Entries)
                .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
                .ThenInclude(x => x.ActivityTags)
                    .ThenInclude(x => x.ActivityTag)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.WeekStart == weekStart, cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetTimesheetsForTeamByManagerAndWeek(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.User)
            .Include(x => x.Entries)
                .ThenInclude(x => x.Project)
            .Where(x =>
                x.WeekStart == weekStart
                && x.User.ManagerHistories.Any(history =>
                    history.ManagerUserId == managerUserId
                    && history.EffectiveToUtc == null)
                && x.User.RoleId == (int)RoleNameEnum.Employee
                && x.User.IsActive)
            .OrderBy(x => x.User.FullName)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
}
