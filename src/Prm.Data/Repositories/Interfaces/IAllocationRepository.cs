using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IAllocationRepository : ICrudBaseRepository<Allocation, int>
{
    Task<Allocation?> GetByIdWithDetails(int allocationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveAllocations(
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveByEmployeeId(
        int employeeId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetActiveByProjectId(
        int projectId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetOverlappingForEmployee(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);

    Task<int> SumUtilizationForEmployeeInPeriod(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingAllocationOnProject(
        int employeeId,
        int projectId,
        DateOnly fromDate,
        DateOnly toDate,
        int? excludeAllocationId = null,
        CancellationToken cancellationToken = default);
}
