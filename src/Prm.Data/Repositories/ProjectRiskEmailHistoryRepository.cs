using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class ProjectRiskEmailHistoryRepository(AppDbContext _dbContext)
    : CrudBaseRepository<ProjectRiskEmailHistory, int>(_dbContext), IProjectRiskEmailHistoryRepository
{
    public Task<bool> ExistsForProjectOnDateAsync(
        int projectId,
        DateOnly sentOnDate,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(
            x => x.ProjectId == projectId && x.SentOnDate == sentOnDate,
            cancellationToken);
}
