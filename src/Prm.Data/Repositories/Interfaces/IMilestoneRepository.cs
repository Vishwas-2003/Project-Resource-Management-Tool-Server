using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IMilestoneRepository : ICrudBaseRepository<Milestone, int>
{
    Task<IReadOnlyList<Milestone>> GetByProjectId(int projectId, CancellationToken cancellationToken = default);
    Task<Milestone?> GetByIdAndProjectId(
        int milestoneId,
        int projectId,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByTitleForProject(
        int projectId,
        string title,
        CancellationToken cancellationToken = default);
}
