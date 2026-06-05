using Prm.Common.Models.Manager;
using Prm.Common.Models.Allocations;

namespace Prm.Api.Services.Interfaces;

public interface IAllocationService
{
    Task<ActiveAllocationsResponse> GetActiveAllocations(
        string? filter,
        CancellationToken cancellationToken = default);

    Task<AllocationCreatedResponse> Create(
        CreateAllocationRequest request,
        int managerUserId,
        CancellationToken cancellationToken = default);

    Task<AllocationEndedResponse> End(
        int allocationId,
        int managerUserId,
        CancellationToken cancellationToken = default);

    Task<ProjectAllocationsResponse> GetByProjectId(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default);
}
