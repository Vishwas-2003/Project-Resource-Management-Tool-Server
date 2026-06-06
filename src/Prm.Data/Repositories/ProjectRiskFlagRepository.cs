using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class ProjectRiskFlagRepository(AppDbContext _dbContext)
    : CrudBaseRepository<ProjectRiskFlag, int>(_dbContext), IProjectRiskFlagRepository
{
    public async Task<IReadOnlyList<ProjectRiskFlag>> GetByProjectId(
        int projectId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task ReplaceForProject(
        int projectId,
        IReadOnlyList<ProjectRiskFlag> flags,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(existing);

        foreach (var flag in flags)
        {
            flag.ProjectId = projectId;
            await DbSet.AddAsync(flag, cancellationToken);
        }
    }
}
