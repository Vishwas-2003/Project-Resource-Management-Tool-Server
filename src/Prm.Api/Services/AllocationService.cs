using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Allocations;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class AllocationService(
    IAllocationRepository _allocationRepository,
    IEmployeeRepository _employeeRepository,
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

            var employeeMatches = allocations
                .Where(x => x.Employee.User.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (employeeMatches.Count > 0)
            {
                allocations = employeeMatches;
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
                EmployeeName = x.Employee.User.FullName,
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

        var employee = await GetAllocatableEmployeeOrThrow(request.EmployeeId, cancellationToken);

        await EnsureNoOverlappingAllocationOnSameProject(
            request.EmployeeId,
            request.ProjectId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        await EnsureUtilizationWithinLimit(
            request.EmployeeId,
            request.UtilizationPercent,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var allocation = new Allocation
        {
            EmployeeId = request.EmployeeId,
            ProjectId = request.ProjectId,
            UtilizationPercent = request.UtilizationPercent,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
        };

        await _allocationRepository.Add(allocation, cancellationToken);
        await _allocationRepository.SaveChanges(cancellationToken);
        await UpdateEmployeeStatus(request.EmployeeId, cancellationToken);

        return new AllocationCreatedResponse
        {
            AllocationId = allocation.Id,
            EmployeeName = employee.User.FullName,
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
        await UpdateEmployeeStatus(allocation.EmployeeId, cancellationToken);

        return new AllocationEndedResponse
        {
            AllocationId = allocation.Id,
            EmployeeName = allocation.Employee.User.FullName,
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
                    EmployeeName = allocation.Employee.User.FullName,
                    UtilizationPercent = allocation.UtilizationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate,
                })
                .ToList(),
        };
    }

    private async Task<Employee> GetAllocatableEmployeeOrThrow(
        int employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetEmployeeDetailById(employeeId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Employee)
        {
            throw new KeyNotFoundException(AppConstants.Manager.EmployeeNotEligible);
        }

        return employee;
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
        int employeeId,
        int projectId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        if (await _allocationRepository.HasOverlappingAllocationOnProject(
                employeeId,
                projectId,
                fromDate,
                toDate,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Allocations.OverlappingAllocationOnProject);
        }
    }

    private async Task EnsureUtilizationWithinLimit(
        int employeeId,
        int utilizationPercent,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var existing = await _allocationRepository.SumUtilizationForEmployeeInPeriod(
            employeeId,
            fromDate,
            toDate,
            cancellationToken: cancellationToken);

        if (existing + utilizationPercent > AllocationConstants.MaxTotalUtilizationPercent)
        {
            throw new InvalidOperationException(AppConstants.Allocations.ExceedsMaxUtilization);
        }
    }

    private async Task UpdateEmployeeStatus(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetById(employeeId, cancellationToken);
        if (employee is null)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await _allocationRepository.SumUtilizationForEmployeeInPeriod(
            employeeId,
            today,
            today,
            cancellationToken: cancellationToken);

        employee.Status = utilization > 0
            ? EmployeeConstants.StatusAllocated
            : EmployeeConstants.StatusBench;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChanges();
    }
}
