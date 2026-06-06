using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

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

    Task<IReadOnlyList<Allocation>> GetPastByEmployeeId(
        EmployeePastAllocationsQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Allocation>> GetOverlappingForEmployee(
        EmployeeAllocationPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<int> SumUtilizationForEmployeeInPeriod(
        EmployeeAllocationPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingAllocationOnProject(
        ProjectAllocationOverlapQuery query,
        CancellationToken cancellationToken = default);
}
