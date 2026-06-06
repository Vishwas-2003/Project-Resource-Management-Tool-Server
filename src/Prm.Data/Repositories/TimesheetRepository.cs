using Microsoft.EntityFrameworkCore;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories;

public class TimesheetRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Timesheet, int>(_dbContext), ITimesheetRepository
{
    public async Task<IReadOnlyList<string>> GetRecentActivityTagNamesForEmployee(
        int employeeId,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default) =>
        await DbContext.TimesheetActivityTags
            .Where(x =>
                x.TimesheetEntry.Timesheet.EmployeeId == employeeId
                && x.TimesheetEntry.Timesheet.WeekStart >= sinceDate)
            .Select(x => x.ActivityTag.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

    public async Task<int> GetHoursWorkedForEmployeeOnProjectInWeek(
        int employeeId,
        int projectId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var hours = await DbContext.TimesheetEntries
            .Where(x =>
                x.Timesheet.EmployeeId == employeeId
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

    public Task<bool> ExistsForEmployeeWeek(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.EmployeeId == employeeId && x.WeekStart == weekStart, cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.WeekStart)
            .ToListAsync(cancellationToken);

    public Task<Timesheet?> GetByEmployeeAndWeek(
        int employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Entries)
                .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
                .ThenInclude(x => x.ActivityTags)
                    .ThenInclude(x => x.ActivityTag)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.WeekStart == weekStart, cancellationToken);

    public async Task<IReadOnlyList<TeamTimesheetEntryRow>> GetEntriesForTeamByManagerAndWeek(
        int managerUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        await DbContext.TimesheetEntries
            .Where(x =>
                x.Timesheet.WeekStart == weekStart
                && x.Timesheet.Employee.ManagerUserId == managerUserId
                && x.Timesheet.Employee.User.RoleId == (int)RoleNameEnum.Employee
                && x.Timesheet.Employee.User.IsActive)
            .Select(x => new TeamTimesheetEntryRow
            {
                EmployeeId = x.Timesheet.EmployeeId,
                EmployeeName = x.Timesheet.Employee.User.FullName,
                ProjectName = x.Project.Name,
                Hours = x.HoursWorked,
                Status = x.Timesheet.Status,
            })
            .OrderBy(x => x.EmployeeName)
            .ThenBy(x => x.ProjectName)
            .ToListAsync(cancellationToken);
}
