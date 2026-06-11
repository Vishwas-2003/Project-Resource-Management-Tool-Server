using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories;

public class AllocationRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Allocation, int>(_dbContext), IAllocationRepository
{
    public Task<Allocation?> GetByIdWithDetails(int allocationId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
            .Include(x => x.Project)
                .ThenInclude(x => x.ManagerUser)
            .FirstOrDefaultAsync(x => x.Id == allocationId, cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveAllocations(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.User)
            .Include(x => x.Project)
            .Where(x => x.FromDate <= asOfDate && x.ToDate >= asOfDate)
            .OrderBy(x => x.User.FullName)
            .ThenBy(x => x.Project.Name)
            .ThenBy(x => x.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByUserId(
        int userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Project)
            .Where(x => x.UserId == userId && x.FromDate <= asOfDate && x.ToDate >= asOfDate)
            .OrderBy(x => x.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByProjectId(
        int projectId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.User)
            .Where(x => x.ProjectId == projectId && x.FromDate <= asOfDate && x.ToDate >= asOfDate)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetPastByUserId(
        UserPastAllocationsQuery query,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Project)
            .Where(x => x.UserId == query.UserId && x.ToDate < query.AsOfDate)
            .OrderByDescending(x => x.ToDate)
            .ThenByDescending(x => x.FromDate)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetOverlappingForUser(
        UserAllocationPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = DbSet.Where(x =>
            x.UserId == query.UserId
            && x.FromDate <= query.ToDate
            && x.ToDate >= query.FromDate);

        if (query.ExcludeAllocationId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Id != query.ExcludeAllocationId.Value);
        }

        return await dbQuery
            .Include(x => x.Project)
            .OrderBy(x => x.Project.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SumUtilizationForUserInPeriod(
        UserAllocationPeriodQuery query,
        CancellationToken cancellationToken = default)
    {
        var allocations = await GetOverlappingForUser(query, cancellationToken);
        return allocations.Sum(x => x.UtilizationPercent);
    }

    public Task<bool> HasOverlappingAllocationOnProject(
        ProjectAllocationOverlapQuery query,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = DbSet.Where(x =>
            x.UserId == query.UserId
            && x.ProjectId == query.ProjectId
            && x.FromDate <= query.ToDate
            && x.ToDate >= query.FromDate);

        if (query.ExcludeAllocationId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Id != query.ExcludeAllocationId.Value);
        }

        return dbQuery.AnyAsync(cancellationToken);
    }
}
