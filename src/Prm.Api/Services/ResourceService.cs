using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class ResourceService(
    IUserRepository _userRepository,
    IAllocationRepository _allocationRepository,
    ITimesheetRepository _timesheetRepository,
    IMapper _mapper) : IResourceService
{
    public async Task<bool> AssignManager(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = request.Department.Trim();
        var designation = request.Designation.Trim();
        if (string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(designation))
        {
            throw new ArgumentException(AppConstants.Resources.DepartmentAndDesignationRequired);
        }

        var resourceUser = await _userRepository.GetByIdWithRole(
            request.ResourceUserId,
            cancellationToken);
        if (resourceUser is null)
        {
            throw new KeyNotFoundException(AppConstants.Resources.UserNotFound);
        }

        if (!resourceUser.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Resources.UserInactive);
        }

        if (resourceUser.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new InvalidOperationException(AppConstants.Resources.InvalidRoleForManagerAssignment);
        }

        var managerUser = await _userRepository.GetByIdWithRole(
            request.ManagerUserId,
            cancellationToken);
        if (managerUser is null
            || !managerUser.IsActive
            || managerUser.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new InvalidOperationException(AppConstants.Resources.InvalidManagerUser);
        }

        resourceUser.Department = department;
        resourceUser.Designation = designation;
        _userRepository.Update(resourceUser);
        await _userRepository.SetManager(resourceUser.Id, managerUser.Id, cancellationToken);

        if (!await _userRepository.HasActiveResourceStatus(resourceUser.Id, cancellationToken))
        {
            await _userRepository.SetCurrentResourceStatus(
                resourceUser.Id,
                (int)ResourceStatusTypeEnum.Bench,
                cancellationToken);
        }

        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<ResourceListResult> GetResources(
        ResourceFilter filter,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetResourceUsers(filter, cancellationToken);
        var summaries = _mapper.Map<List<ResourceSummary>>(users);
        for (var rowIndex = 0; rowIndex < summaries.Count; rowIndex++)
        {
            summaries[rowIndex].RowNumber = rowIndex + 1;
        }

        return new ResourceListResult
        {
            Resources = summaries,
            Total = summaries.Count,
            Allocated = summaries.Count(summary => summary.Status == ResourceConstants.StatusAllocated),
            Bench = summaries.Count(summary => summary.Status == ResourceConstants.StatusBench),
        };
    }

    public async Task<bool> Update(
        int resourceUserId,
        UpdateResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(resourceUserId, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Resources.AlreadyDeactivated);
        }

        _mapper.Map(request, user);
        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> Deactivate(int resourceUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserDetailOrThrow(resourceUserId, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Resources.AlreadyDeactivated);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = user.Allocations.Where(allocation => allocation.ToDate >= today).ToList();

        foreach (var allocation in activeAllocations)
        {
            allocation.ToDate = today;
        }

        if (user.RoleId == (int)RoleNameEnum.Employee)
        {
            await _userRepository.SetCurrentResourceStatus(
                user.Id,
                (int)ResourceStatusTypeEnum.Bench,
                cancellationToken);
        }

        user.IsActive = false;

        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<ResourceDetailResponse> GetDetail(
        int resourceUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourcePoolUserOrThrow(resourceUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(resourceUserId, today, cancellationToken);
        var allocations = await _allocationRepository.GetActiveByUserId(resourceUserId, today, cancellationToken);
        var pastAllocations = await _allocationRepository.GetPastByUserId(
            new UserPastAllocationsQuery
            {
                UserId = resourceUserId,
                AsOfDate = today,
                Limit = ManagerConstants.PastAllocationsDisplayCount,
            },
            cancellationToken);
        var sinceDate = today.AddDays(-7 * ManagerConstants.ActivityTagsLookbackWeeks);
        var activityTags = await _timesheetRepository.GetRecentActivityTagNamesForUser(
            resourceUserId,
            sinceDate,
            cancellationToken);

        return new ResourceDetailResponse
        {
            Id = user.Id,
            Name = user.FullName,
            Department = user.Department,
            CurrentStatus = FormatResourceStatus(utilization),
            UtilizationPercent = utilization,
            ProfileSkills = FormatSkills(user),
            ActiveAllocations = MapAllocationRows(allocations),
            PastAllocations = MapAllocationRows(pastAllocations),
            RecentActivityTags = activityTags,
        };
    }

    public async Task<ResourceUtilizationResponse> GetUtilization(
        int resourceUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourcePoolUserOrThrow(resourceUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(resourceUserId, today, cancellationToken);

        return new ResourceUtilizationResponse
        {
            ResourceUserId = user.Id,
            Name = user.FullName,
            UtilizationPercent = utilization,
            StatusDescription = utilization == 0
                ? ManagerConstants.AvailabilityOnBench
                : $"{utilization}%",
        };
    }

    private async Task<User> GetResourcePoolUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetResourceUserDetailById(userId, cancellationToken);
        if (user is null || user.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ResourceNotFound);
        }

        return user;
    }

    private Task<int> GetUtilizationOnDate(int userId, DateOnly date, CancellationToken cancellationToken) =>
        _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = userId,
                FromDate = date,
                ToDate = date,
            },
            cancellationToken);

    private static IReadOnlyList<ResourceAllocationRow> MapAllocationRows(IEnumerable<Allocation> allocations) =>
        allocations
            .Select(allocation => new ResourceAllocationRow
            {
                Project = allocation.Project.Name,
                UtilizationPercent = allocation.UtilizationPercent,
                FromDate = allocation.FromDate,
                ToDate = allocation.ToDate,
            })
            .ToList();

    private static string FormatSkills(User user) =>
        string.Join(
            ", ",
            user.UserSkills
                .Select(skillAssignment => skillAssignment.Skill.Name)
                .OrderBy(skillName => skillName));

    private static string FormatResourceStatus(int utilization) =>
        utilization > 0
            ? ResourceConstants.StatusAllocated
            : ResourceConstants.StatusBench;

    private async Task<User> GetResourceUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Resources.NotFound);
        }

        return user;
    }

    private async Task<User> GetResourceUserDetailOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetResourceUserDetailById(userId, cancellationToken)
            ?? await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Resources.NotFound);
        }

        return user;
    }
}
