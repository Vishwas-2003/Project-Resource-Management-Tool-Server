using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class ManagerService(
    IUserRepository _userRepository,
    IAllocationRepository _allocationRepository) : IManagerService
{
    public async Task<ResourceDashboardResponse> GetResourceDashboard(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureManagerUserOrThrow(managerUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var users = await _userRepository.GetResourcePoolUsers(cancellationToken);

        var bench = new List<BenchEmployeeRow>();
        var active = new List<ActiveEmployeeRow>();
        var partial = 0;

        foreach (var user in users)
        {
            var utilization = await GetUtilizationOnDate(user.Id, today, cancellationToken);
            if (utilization <= 0)
            {
                bench.Add(new BenchEmployeeRow
                {
                    Id = user.Id,
                    Name = user.FullName,
                    Department = user.Department,
                    Skills = FormatSkills(user),
                });
                continue;
            }

            else if (utilization < AllocationConstants.MaxTotalUtilizationPercent)
            {
                partial++;
            }

            active.Add(new ActiveEmployeeRow
            {
                Id = user.Id,
                Name = user.FullName,
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

    private Task<int> GetUtilizationOnDate(int userId, DateOnly date, CancellationToken cancellationToken) =>
        _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = userId,
                FromDate = date,
                ToDate = date,
            },
            cancellationToken);

    private static string FormatSkills(User user) =>
        string.Join(
            ", ",
            user.UserSkills
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
