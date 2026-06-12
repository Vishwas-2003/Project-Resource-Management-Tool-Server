using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IEmailNotificationHistoryRepository : ICrudBaseRepository<EmailNotificationHistory, int>
{
    Task<bool> ExistsForProjectRiskOnDateAsync(
        int projectId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForMissedTimesheetOnDateAsync(
        int userId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default);
}
