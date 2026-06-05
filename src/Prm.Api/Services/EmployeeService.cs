using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class EmployeeService(
    IEmployeeRepository _employeeRepository,
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

        var employeeUser = await _userRepository.GetByIdWithRoleAndEmployee(
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

        var managerUser = await _userRepository.GetByIdWithRoleAndEmployee(
            request.ManagerUserId,
            cancellationToken);
        if (managerUser is null
            || !managerUser.IsActive
            || managerUser.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new InvalidOperationException(AppConstants.Employees.InvalidManagerUser);
        }

        var employee = employeeUser.Employee;
        if (employee is null)
        {
            employee = new Employee
            {
                UserId = employeeUser.Id,
                Department = department,
                Designation = designation,
                ManagerUserId = managerUser.Id,
                Status = EmployeeConstants.StatusBench,
            };
            await _employeeRepository.Add(employee, cancellationToken);
        }
        else
        {
            employee.ManagerUserId = managerUser.Id;
            employee.Department = department;
            employee.Designation = designation;
            _employeeRepository.Update(employee);
        }

        await _employeeRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<EmployeeListResult> GetEmployees(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetEmployees(filter, cancellationToken);
        var summaries = _mapper.Map<List<EmployeeSummary>>(employees);
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
        var employee = await GetEmployeeOrThrow(employeeId, cancellationToken);

        if (!employee.User.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
        }

        _mapper.Map(request, employee);
        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> Deactivate(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrow(employeeId, cancellationToken);

        if (!employee.User.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Employees.AlreadyDeactivated);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocations = employee.Allocations.Where(allocation => allocation.ToDate >= today).ToList();

        foreach (var allocation in activeAllocations)
        {
            allocation.ToDate = today;
        }

        if (employee.User.RoleId == (int)RoleNameEnum.Employee)
        {
            employee.Status = EmployeeConstants.StatusBench;
        }

        employee.User.IsActive = false;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<EmployeeDetailResponse> GetDetail(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetResourceEmployeeOrThrow(employeeId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(employeeId, today, cancellationToken);
        var allocations = await _allocationRepository.GetActiveByEmployeeId(employeeId, today, cancellationToken);
        var sinceDate = today.AddDays(-7 * ManagerConstants.ActivityTagsLookbackWeeks);
        var activityTags = await _timesheetRepository.GetRecentActivityTagNamesForEmployee(
            employeeId,
            sinceDate,
            cancellationToken);

        return new EmployeeDetailResponse
        {
            Id = employee.Id,
            Name = employee.User.FullName,
            Department = employee.Department,
            CurrentStatus = FormatEmployeeStatus(utilization),
            UtilizationPercent = utilization,
            ProfileSkills = FormatSkills(employee),
            ActiveAllocations = allocations
                .Select(allocation => new EmployeeAllocationRow
                {
                    Project = allocation.Project.Name,
                    UtilizationPercent = allocation.UtilizationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate,
                })
                .ToList(),
            RecentActivityTags = activityTags,
        };
    }

    public async Task<EmployeeUtilizationResponse> GetUtilization(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetResourceEmployeeOrThrow(employeeId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await GetUtilizationOnDate(employeeId, today, cancellationToken);

        return new EmployeeUtilizationResponse
        {
            EmployeeId = employee.Id,
            Name = employee.User.FullName,
            UtilizationPercent = utilization,
            StatusDescription = utilization == 0
                ? ManagerConstants.AvailabilityOnBench
                : $"{utilization}%",
        };
    }

    private async Task<Employee> GetResourceEmployeeOrThrow(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetEmployeeDetailById(employeeId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Manager.EmployeeNotFound);
        }

        return employee;
    }

    private Task<int> GetUtilizationOnDate(int employeeId, DateOnly date, CancellationToken cancellationToken) =>
        _allocationRepository.SumUtilizationForEmployeeInPeriod(
            employeeId,
            date,
            date,
            cancellationToken: cancellationToken);

    private static string FormatSkills(Employee employee) =>
        string.Join(
            ", ",
            employee.EmployeeSkills
                .Select(skillAssignment => skillAssignment.Skill.Name)
                .OrderBy(skillName => skillName));

    private static string FormatEmployeeStatus(int utilization) =>
        utilization > 0
            ? $"{EmployeeConstants.StatusAllocated} ({utilization}%)"
            : EmployeeConstants.StatusBench;

    private async Task<Employee> GetEmployeeOrThrow(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetById(employeeId, cancellationToken);
        if (employee is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return employee;
    }

}
