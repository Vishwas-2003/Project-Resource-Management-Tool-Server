using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Allocations;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class AllocationService(
    IAllocationRepository _allocationRepository,
    IUserRepository _userRepository,
    IProjectRepository _projectRepository) : IAllocationService
{
    public async Task<ActiveAllocationsResponse> GetActiveAllocations(
        string? filter,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocations = await _allocationRepository.GetActiveAllocations(today, cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var query = filter.Trim();

            var resourceMatches = allocations
                .Where(x => x.User.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (resourceMatches.Count > 0)
            {
                allocations = resourceMatches;
            }
            else
            {
                var projectMatches = allocations
                    .Where(x => x.Project.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (projectMatches.Count > 0)
                {
                    allocations = projectMatches;
                }
                else
                {
                    throw new ArgumentException(AppConstants.Allocations.InvalidFilter);
                }
            }
        }

        return new ActiveAllocationsResponse
        {
            TotalActiveAllocations = allocations.Count,
            Allocations = allocations.Select(x => new ActiveAllocationRow
            {
                ResourceName = x.User.FullName,
                ProjectName = x.Project.Name,
                UtilizationPercent = x.UtilizationPercent,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
            }).ToList(),
        };
    }

    public async Task<AllocationCreatedResponse> Create(
        CreateAllocationRequest request,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(request.FromDate, request.ToDate);
        EnsureAllocationDatesNotInPast(request.FromDate, request.ToDate);
        ValidateUtilizationPercent(request.UtilizationPercent);

        var project = await GetOwnedProjectOrThrow(request.ProjectId, managerUserId, cancellationToken);
        EnsureProjectAllowsAllocation(project);
        EnsureAllocationDatesWithinProject(project, request.FromDate, request.ToDate);

        var user = await GetAllocatableUserOrThrow(request.ResourceUserId, cancellationToken);
        await EnsureResourceUnderManager(request.ResourceUserId, managerUserId, cancellationToken);
        UserAvailabilityHelper.EnsureAllocationDatesEligibleForUser(user, request.FromDate, request.ToDate);

        await EnsureNoOverlappingAllocationOnSameProject(
            request.ResourceUserId,
            request.ProjectId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        await EnsureUtilizationWithinLimit(
            request.ResourceUserId,
            request.UtilizationPercent,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var allocation = new Allocation
        {
            UserId = request.ResourceUserId,
            ProjectId = request.ProjectId,
            UtilizationPercent = request.UtilizationPercent,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
        };

        await _allocationRepository.Add(allocation, cancellationToken);
        await _allocationRepository.SaveChanges(cancellationToken);
        await UpdateUserResourceStatus(request.ResourceUserId, cancellationToken);

        return new AllocationCreatedResponse
        {
            AllocationId = allocation.Id,
            ResourceName = user.FullName,
            ProjectName = project.Name,
            UtilizationPercent = allocation.UtilizationPercent,
            FromDate = allocation.FromDate,
            ToDate = allocation.ToDate,
        };
    }

    public async Task<AllocationEndedResponse> End(
        int allocationId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var allocation = await _allocationRepository.GetByIdWithDetails(allocationId, cancellationToken);
        if (allocation is null)
        {
            throw new KeyNotFoundException(AppConstants.Allocations.NotFound);
        }

        if (allocation.Project.ManagerUserId != managerUserId)
        {
            throw new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (allocation.ToDate < today)
        {
            throw new InvalidOperationException(AppConstants.Allocations.AlreadyEnded);
        }

        allocation.ToDate = today;
        _allocationRepository.Update(allocation);
        await _allocationRepository.SaveChanges(cancellationToken);
        await UpdateUserResourceStatus(allocation.UserId, cancellationToken);

        return new AllocationEndedResponse
        {
            AllocationId = allocation.Id,
            ResourceName = allocation.User.FullName,
            ProjectName = allocation.Project.Name,
            EndDate = today,
        };
    }

    public async Task<ProjectAllocationsResponse> GetByProjectId(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedProjectOrThrow(projectId, managerUserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocations = await _allocationRepository.GetActiveByProjectId(projectId, today, cancellationToken);

        return new ProjectAllocationsResponse
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Allocations = allocations
                .Select((allocation, rowIndex) => new ProjectAllocationRow
                {
                    AllocationId = allocation.Id,
                    RowNumber = rowIndex + 1,
                    ResourceName = allocation.User.FullName,
                    UtilizationPercent = allocation.UtilizationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate,
                })
                .ToList(),
        };
    }
}
