using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Data.Repositories.Interfaces;

public interface IAllocationRepository : ICrudBaseRepository<Allocation, int>
{
    Task<Allocation?> GetByIdWithDetails(int allocationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveAllocations(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveByUserId(
        int userId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveByProjectId(
        int projectId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetPastByUserId(
        UserPastAllocationsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetOverlappingForUser(
        UserAllocationPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<int> SumUtilizationForUserInPeriod(
        UserAllocationPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingAllocationOnProject(
        ProjectAllocationOverlapQuery query,
        CancellationToken cancellationToken = default);
}
