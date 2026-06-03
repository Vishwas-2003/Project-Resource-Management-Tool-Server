using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class ManagerService(
    IEmployeeRepository _employeeRepository,
    IAllocationRepository _allocationRepository) : IManagerService
{
    public async Task<ResourceDashboardResponse> GetResourceDashboard(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetManagerEmployeeOrThrow(managerUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = await _employeeRepository.GetResourcePoolEmployees(cancellationToken);

        var bench = new List<BenchEmployeeRow>();
        var active = new List<ActiveEmployeeRow>();
        var overUtilised = 0;
        var partial = 0;

        foreach (var employee in employees)
        {
            var utilization = await GetUtilizationOnDate(employee.Id, today, cancellationToken);
            if (utilization <= 0)
            {
                bench.Add(new BenchEmployeeRow
                {
                    Id = employee.Id,
                    Name = employee.User.FullName,
                    Department = employee.Department,
                    Skills = FormatSkills(employee),
                });
                continue;
            }

            if (utilization > AllocationConstants.MaxTotalUtilizationPercent)
            {
                overUtilised++;
            }
            else if (utilization < AllocationConstants.MaxTotalUtilizationPercent)
            {
                partial++;
            }

            active.Add(new ActiveEmployeeRow
            {
                Id = employee.Id,
                Name = employee.User.FullName,
                AllocationPercent = utilization,
                Availability = FormatAvailability(utilization),
            });
        }

        return new ResourceDashboardResponse
        {
            PeriodLabel = today.ToString("MMMM yyyy"),
            BenchEmployees = bench,
            ActiveEmployees = active,
            Summary = new ResourceDashboardSummary
            {
                BenchCount = bench.Count,
                OverUtilisedCount = overUtilised,
                PartialCount = partial,
            },
        };
    }

    private async Task<Employee> GetManagerEmployeeOrThrow(int userId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetEmployeeByUserId(userId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
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

    private static string FormatAvailability(int utilization)
    {
        if (utilization >= AllocationConstants.MaxTotalUtilizationPercent)
        {
            return ManagerConstants.AvailabilityFull;
        }

        var free = AllocationConstants.MaxTotalUtilizationPercent - utilization;
        return $"{free}% free";
    }
}
