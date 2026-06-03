using Microsoft.EntityFrameworkCore;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class TimesheetRepository(AppDbContext dbContext) : ITimesheetRepository
{
    public async Task<IReadOnlyList<string>> GetRecentActivityTagNamesForEmployee(
        int employeeId,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.TimesheetActivityTags
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
        var hours = await dbContext.TimesheetEntries
            .Where(x =>
                x.Timesheet.EmployeeId == employeeId
                && x.ProjectId == projectId
                && x.Timesheet.WeekStart == weekStart)
            .SumAsync(x => (int?)x.HoursWorked, cancellationToken);

        return hours ?? 0;
    }
}
