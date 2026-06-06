using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class ProjectHealthService(
    ITimesheetRepository _timesheetRepository,
    ISystemConfigurationRepository _systemConfigurationRepository) : IProjectHealthService
{
    public async Task<ProjectHealthEvaluation> Evaluate(
        Project project,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxWeeklyHours = await GetMaxWeeklyHours(cancellationToken);
        var riskFlags = await BuildRiskFlags(project, today, maxWeeklyHours, cancellationToken);
        var healthStatus = DeriveHealthStatus(project, riskFlags, today);

        return new ProjectHealthEvaluation
        {
            HealthStatus = healthStatus,
            RiskFlags = riskFlags,
        };
    }

    private static string DeriveHealthStatus(
        Project project,
        IReadOnlyList<RiskFlagItem> riskFlags,
        DateOnly today)
    {
        var failures = riskFlags.Count(flag => flag.Outcome == ManagerConstants.RiskFlagFail);

        if (failures >= ManagerConstants.RiskFlagCountForProjectUnderRisk)
        {
            return ManagerConstants.HealthAtRisk;
        }

        if (failures == ManagerConstants.RiskFlagCountForProjectNeedAttention)
        {
            return ManagerConstants.HealthAttention;
        }

        var hasOverdue = project.Milestones.Any(milestone => IsMilestoneOverdue(milestone, today));
        if (hasOverdue)
        {
            return ManagerConstants.HealthAttention;
        }

        return ManagerConstants.HealthOnTrack;
    }

    private async Task<IReadOnlyList<RiskFlagItem>> BuildRiskFlags(
        Project project,
        DateOnly today,
        int maxWeeklyHours,
        CancellationToken cancellationToken)
    {
        var flags = new List<RiskFlagItem>();
        var overdueMilestone = project.Milestones
            .Where(milestone => IsMilestoneOverdue(milestone, today))
            .OrderBy(milestone => milestone.DueDate)
            .FirstOrDefault();

        if (overdueMilestone is not null)
        {
            var daysOverdue = today.DayNumber - overdueMilestone.DueDate.DayNumber;
            flags.Add(new RiskFlagItem
            {
                Outcome = ManagerConstants.RiskFlagFail,
                Message = $"{overdueMilestone.Title} milestone is {daysOverdue} days overdue",
            });
        }

        var lastWeekStart = TimesheetWeekHelper.GetWeekStart(today).AddDays(-7);
        var activeAllocations = project.Allocations
            .Where(allocation => allocation.FromDate <= today && allocation.ToDate >= today)
            .ToList();

        foreach (var allocation in activeAllocations)
        {
            var expectedHours = TimesheetWeekHelper.ComputeExpectedHours(
                allocation.UtilizationPercent,
                maxWeeklyHours);
            var actualHours = await _timesheetRepository.GetHoursWorkedForEmployeeOnProjectInWeek(
                allocation.EmployeeId,
                project.Id,
                lastWeekStart,
                cancellationToken);

            if (expectedHours > 0 && actualHours < expectedHours)
            {
                flags.Add(new RiskFlagItem
                {
                    Outcome = ManagerConstants.RiskFlagFail,
                    Message =
                        $"{allocation.Employee.User.FullName} logged only {actualHours} hrs last week (expected {expectedHours} hrs)",
                });
                break;
            }
        }

        var totalAllocation = activeAllocations.Sum(x => x.UtilizationPercent);
        var allocationOk = totalAllocation > AllocationConstants.MinTotalUtilizationPercent
            && totalAllocation <= AllocationConstants.MaxTotalUtilizationPercent;
        flags.Add(new RiskFlagItem
        {
            Outcome = allocationOk ? ManagerConstants.RiskFlagPass : ManagerConstants.RiskFlagFail,
            Message = allocationOk
                ? ManagerConstants.ResourcesCorrectlyAllocated
                : ManagerConstants.ProjectResourcesNeedAttention,
        });

        return flags;
    }

    private async Task<int> GetMaxWeeklyHours(CancellationToken cancellationToken)
    {
        var configuration = await _systemConfigurationRepository.GetById(
            (int)ConfigurationOptionEnum.MaxWeeklyHours,
            cancellationToken);

        if (configuration is null
            || !int.TryParse(configuration.Value, out var hours)
            || hours <= 0)
        {
            return ManagerConstants.DefaultMaxWeeklyHours;
        }

        return hours;
    }

    private static bool IsMilestoneOverdue(Milestone milestone, DateOnly today) =>
        milestone.Status != MilestoneConstants.StatusDone && milestone.DueDate < today;
}
