using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Allocations;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class AllocationService(
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
        ValidateUtilizationPercent(request.UtilizationPercent);

        var project = await GetOwnedProjectOrThrow(request.ProjectId, managerUserId, cancellationToken);
        EnsureProjectAllowsAllocation(project);
        EnsureAllocationDatesWithinProject(project, request.FromDate, request.ToDate);

        var user = await GetAllocatableUserOrThrow(request.ResourceUserId, cancellationToken);
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

    private async Task<User> GetAllocatableUserOrThrow(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetResourceUserDetailById(userId, cancellationToken);
        if (user is null || user.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ResourceNotEligible);
        }

        return user;
    }

    private async Task<Project> GetOwnedProjectOrThrow(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithManager(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        if (project.ManagerUserId != managerUserId)
        {
            throw new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned);
        }

        return project;
    }

    private static void EnsureProjectAllowsAllocation(Project project)
    {
        if (project.Status is not ProjectConstants.StatusActive and not ProjectConstants.StatusPlanned)
        {
            throw new InvalidOperationException(AppConstants.Allocations.ProjectNotAllocatable);
        }
    }

    private static void EnsureAllocationDatesWithinProject(
        Project project,
        DateOnly fromDate,
        DateOnly toDate)
    {
        if (fromDate < project.StartDate || toDate > project.EndDate)
        {
            throw new ArgumentException(AppConstants.Allocations.AllocationDatesOutsideProject);
        }
    }

    private static void ValidateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate >= toDate)
        {
            throw new ArgumentException(AppConstants.Allocations.InvalidDateRange);
        }
    }

    private static void ValidateUtilizationPercent(int utilizationPercent)
    {
        if (utilizationPercent is < AllocationConstants.MinUtilizationPercent
            or > AllocationConstants.MaxUtilizationPercent)
        {
            throw new ArgumentException(AppConstants.Allocations.InvalidUtilization);
        }
    }

    private async Task EnsureNoOverlappingAllocationOnSameProject(
        int userId,
        int projectId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        if (await _allocationRepository.HasOverlappingAllocationOnProject(
                new ProjectAllocationOverlapQuery
                {
                    UserId = userId,
                    ProjectId = projectId,
                    FromDate = fromDate,
                    ToDate = toDate,
                },
                cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Allocations.OverlappingAllocationOnProject);
        }
    }

    private async Task EnsureUtilizationWithinLimit(
        int userId,
        int utilizationPercent,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var existing = await _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
            },
            cancellationToken);

        if (existing + utilizationPercent > AllocationConstants.MaxTotalUtilizationPercent)
        {
            throw new InvalidOperationException(AppConstants.Allocations.ExceedsMaxUtilization);
        }
    }

    private async Task UpdateUserResourceStatus(int userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = userId,
                FromDate = today,
                ToDate = today,
            },
            cancellationToken);

        var status = utilization > 0
            ? ResourceStatusTypeEnum.Allocated
            : ResourceStatusTypeEnum.Bench;

        await _userRepository.SetCurrentResourceStatus(userId, (int)status, cancellationToken);
        await _userRepository.SaveChanges(cancellationToken);
    }
}
