using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class AllocationService
{
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

    private async Task EnsureResourceUnderManager(
        int resourceUserId,
        int managerUserId,
        CancellationToken cancellationToken)
    {
        if (!await _userRepository.IsResourceManagedByManager(
                resourceUserId,
                managerUserId,
                cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Manager.ResourceNotUnderManager);
        }
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

    private static void EnsureAllocationDatesNotInPast(DateOnly fromDate, DateOnly toDate)
    {
        DateValidationHelper.EnsureNotBeforeToday(fromDate, AppConstants.Allocations.PastDateNotAllowed);
        DateValidationHelper.EnsureNotBeforeToday(toDate, AppConstants.Allocations.PastDateNotAllowed);
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
