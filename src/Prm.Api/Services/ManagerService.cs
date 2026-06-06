using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class ManagerService(
    IUserRepository _userRepository,
    IEmployeeRepository _employeeRepository,
    IAllocationRepository _allocationRepository) : IManagerService
{
    public async Task<ResourceDashboardResponse> GetResourceDashboard(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureManagerUserOrThrow(managerUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employees = await _employeeRepository.GetResourcePoolEmployees(cancellationToken);

        var bench = new List<BenchEmployeeRow>();
        var active = new List<ActiveEmployeeRow>();
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

        AssignRowNumber(bench, active);

        return new ResourceDashboardResponse
        {
            PeriodLabel = today.ToString("MMMM yyyy"),
            BenchEmployees = bench,
            ActiveEmployees = active,
            Summary = new ResourceDashboardSummary
            {
                BenchCount = bench.Count,
                PartialCount = partial,
            },
        };
    }

    private void AssignRowNumber(List<BenchEmployeeRow> bench, List<ActiveEmployeeRow> active)
    {
        int rowNumber = 1;

        foreach(BenchEmployeeRow employeeRow in bench)
        {
            employeeRow.RowNumber = rowNumber++;
        }

        foreach(ActiveEmployeeRow employeeRow in active)
        {
            employeeRow.RowNumber = rowNumber++;
        }
    }

    private async Task EnsureManagerUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var manager = await _userRepository.GetActiveManagerById(userId, cancellationToken);
        if (manager is null)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }
    }

    private Task<int> GetUtilizationOnDate(int employeeId, DateOnly date, CancellationToken cancellationToken) =>
        _allocationRepository.SumUtilizationForEmployeeInPeriod(
            new EmployeeAllocationPeriodQuery
            {
                EmployeeId = employeeId,
                FromDate = date,
                ToDate = date,
            },
            cancellationToken);

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
