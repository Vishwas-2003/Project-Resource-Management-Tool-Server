using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class ProjectRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Project, int>(_dbContext), IProjectRepository
{
    public override Task<Project?> GetById(int projectId, CancellationToken cancellationToken = default) =>
        GetByIdWithManager(projectId, cancellationToken);

    public Task<Project?> GetByIdWithManager(int projectId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.ManagerUser)
            .Include(x => x.Milestones)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetAllWithManager(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.ManagerUser)
            .Include(x => x.Milestones)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<Project?> GetByIdWithDetails(int projectId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.ManagerUser)
            .Include(x => x.Milestones)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<Project>> GetByManagerUserId(
        int managerUserId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Milestones)
            .Include(x => x.Allocations)
                .ThenInclude(x => x.User)
            .Where(x => x.ManagerUserId == managerUserId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByName(string name, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Name == name.Trim(), cancellationToken);

    public Task<bool> ExistsByName(string name, int excludeProjectId, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Name == name.Trim() && x.Id != excludeProjectId, cancellationToken);
}
