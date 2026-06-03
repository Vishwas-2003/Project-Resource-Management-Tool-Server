using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface ITimesheetRepository
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
}
