using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IProjectRiskEmailHistoryRepository : ICrudBaseRepository<ProjectRiskEmailHistory, int>
{
    Task<bool> ExistsForProjectOnDateAsync(
        int projectId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default);
}
