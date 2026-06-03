using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class AllocationRepository(AppDbContext _dbContext)
    : CrudBaseRepository<Allocation, int>(_dbContext), IAllocationRepository
{
    public Task<Allocation?> GetByIdWithDetails(int allocationId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Employee)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
                .ThenInclude(x => x.ManagerEmployee)
            .FirstOrDefaultAsync(x => x.Id == allocationId, cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByEmployeeId(
        int employeeId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Project)
            .Where(x => x.EmployeeId == employeeId && x.FromDate <= asOfDate && x.ToDate >= asOfDate)
            .OrderBy(x => x.FromDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetActiveByProjectId(
        int projectId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Employee)
                .ThenInclude(x => x.User)
            .Where(x => x.ProjectId == projectId && x.FromDate <= asOfDate && x.ToDate >= asOfDate)
            .OrderBy(x => x.Employee.User.FullName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> GetOverlappingForEmployee(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(x =>
            x.EmployeeId == employeeId
            && x.FromDate <= toDate
            && x.ToDate >= fromDate);

        if (excludeAllocationId.HasValue)
        {
            query = query.Where(x => x.Id != excludeAllocationId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> SumUtilizationForEmployeeInPeriod(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var allocations = await GetOverlappingForEmployee(
            employeeId,
            fromDate,
            toDate,
            excludeAllocationId,
            cancellationToken);

        return allocations.Sum(x => x.UtilizationPercent);
    }

    public Task<bool> HasOverlappingAllocationOnProject(
        int employeeId,
        int projectId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(x =>
            x.EmployeeId == employeeId
            && x.ProjectId == projectId
            && x.FromDate <= toDate
            && x.ToDate >= fromDate);

        if (excludeAllocationId.HasValue)
        {
            query = query.Where(x => x.Id != excludeAllocationId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }
}
