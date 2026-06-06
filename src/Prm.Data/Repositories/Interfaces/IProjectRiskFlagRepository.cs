using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IProjectRiskFlagRepository : ICrudBaseRepository<ProjectRiskFlag, int>
{
    Task<IReadOnlyList<ProjectRiskFlag>> GetByProjectId(
        int projectId,
        CancellationToken cancellationToken = default);

    Task ReplaceForProject(
        int projectId,
        IReadOnlyList<ProjectRiskFlag> flags,
        CancellationToken cancellationToken = default);
}
