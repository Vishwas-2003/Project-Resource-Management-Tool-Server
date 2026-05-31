using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class MilestoneRepository(AppDbContext dbContext)
    : CrudBaseRepository<Milestone, int>(dbContext), IMilestoneRepository
{
    public async Task<IReadOnlyList<Milestone>> GetByProjectId(
        int projectId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<Milestone?> GetByIdAndProjectId(
        int milestoneId,
        int projectId,
        CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(
            x => x.Id == milestoneId && x.ProjectId == projectId,
            cancellationToken);

    public Task<bool> ExistsByTitleForProject(
        int projectId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim();
        return DbSet.AnyAsync(
            x => x.ProjectId == projectId && x.Title == normalizedTitle,
            cancellationToken);
    }
}
