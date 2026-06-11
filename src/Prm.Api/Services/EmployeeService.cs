using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class EmployeeService(
    IUserRepository _userRepository,
    IAllocationRepository _allocationRepository,
    ITimesheetRepository _timesheetRepository,
    IMapper _mapper) : IEmployeeService
{
    public async Task<bool> AssignManager(
        AssignManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = request.Department.Trim();
        var designation = request.Designation.Trim();
        if (string.IsNullOrWhiteSpace(department) || string.IsNullOrWhiteSpace(designation))
        {
            throw new ArgumentException(AppConstants.Employees.DepartmentAndDesignationRequired);
        }

        var employeeUser = await _userRepository.GetByIdWithRole(
            request.EmployeeUserId,
            cancellationToken);
        if (employeeUser is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.UserNotFound);
        }

        if (!employeeUser.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.UserInactive);
        }

        if (employeeUser.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new InvalidOperationException(AppConstants.Employees.InvalidRoleForManagerAssignment);
        }

        var managerUser = await _userRepository.GetByIdWithRole(
            request.ManagerUserId,
            cancellationToken);
        if (managerUser is null
            || !managerUser.IsActive
            || managerUser.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new InvalidOperationException(AppConstants.Employees.InvalidManagerUser);
        }

        employeeUser.Department = department;
        employeeUser.Designation = designation;
        _userRepository.Update(employeeUser);
        await _userRepository.SetManager(employeeUser.Id, managerUser.Id, cancellationToken);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<EmployeeListResult> GetEmployees(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetEmployeeUsers(filter, cancellationToken);
        var summaries = _mapper.Map<List<EmployeeSummary>>(users);
        for (var rowIndex = 0; rowIndex < summaries.Count; rowIndex++)
        {
            summaries[rowIndex].RowNumber = rowIndex + 1;
        }

        return new EmployeeListResult
        {
            Employees = summaries,
            Total = summaries.Count,
            Allocated = summaries.Count(summary => summary.Status == EmployeeConstants.StatusAllocated),
            Bench = summaries.Count(summary => summary.Status == EmployeeConstants.StatusBench),
        };
    }

    public async Task<bool> Update(
        int employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetEmployeeUserOrThrow(employeeId, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
        }

        _mapper.Map(request, user);
        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> Deactivate(int employeeId, CancellationToken cancellationToken = default)
    {
        var user = await GetEmployeeUserDetailOrThrow(employeeId, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
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

    public async Task<EmployeeDetailResponse> GetDetail(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceEmployeeOrThrow(employeeId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(employeeId, today, cancellationToken);
        var allocations = await _allocationRepository.GetActiveByUserId(employeeId, today, cancellationToken);
        var pastAllocations = await _allocationRepository.GetPastByUserId(
            new UserPastAllocationsQuery
            {
                UserId = employeeId,
                AsOfDate = today,
                Limit = ManagerConstants.PastAllocationsDisplayCount,
            },
            cancellationToken);
        var sinceDate = today.AddDays(-7 * ManagerConstants.ActivityTagsLookbackWeeks);
        var activityTags = await _timesheetRepository.GetRecentActivityTagNamesForUser(
            employeeId,
            sinceDate,
            cancellationToken);

        return new EmployeeDetailResponse
        {
            Id = user.Id,
            Name = user.FullName,
            Department = user.Department,
            CurrentStatus = FormatEmployeeStatus(utilization),
            UtilizationPercent = utilization,
            ProfileSkills = FormatSkills(user),
            ActiveAllocations = MapAllocationRows(allocations),
            PastAllocations = MapAllocationRows(pastAllocations),
            RecentActivityTags = activityTags,
        };
    }

    public async Task<EmployeeUtilizationResponse> GetUtilization(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetResourceEmployeeOrThrow(employeeId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(employeeId, today, cancellationToken);

        return new EmployeeUtilizationResponse
        {
            EmployeeId = user.Id,
            Name = user.FullName,
            UtilizationPercent = utilization,
            StatusDescription = utilization == 0
                ? ManagerConstants.AvailabilityOnBench
                : $"{utilization}%",
        };
    }

    private async Task<User> GetResourceEmployeeOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetEmployeeUserDetailById(userId, cancellationToken);
        if (user is null || user.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Manager.EmployeeNotFound);
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

    private static IReadOnlyList<EmployeeAllocationRow> MapAllocationRows(IEnumerable<Allocation> allocations) =>
        allocations
            .Select(allocation => new EmployeeAllocationRow
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

    private static string FormatEmployeeStatus(int utilization) =>
        utilization > 0
            ? EmployeeConstants.StatusAllocated
            : EmployeeConstants.StatusBench;

    private async Task<User> GetEmployeeUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return user;
    }

    private async Task<User> GetEmployeeUserDetailOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetEmployeeUserDetailById(userId, cancellationToken)
            ?? await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return user;
    }
}
