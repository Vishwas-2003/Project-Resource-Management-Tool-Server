using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IProjectRepository : ICrudBaseRepository<Project, int>
{
    Task<Project?> GetByIdWithManager(int projectId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithDetails(int projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllWithManager(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByManagerUserId(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByName(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsByName(string name, int excludeProjectId, CancellationToken cancellationToken = default);
}
