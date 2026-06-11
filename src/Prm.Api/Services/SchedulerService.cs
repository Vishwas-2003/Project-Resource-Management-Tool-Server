using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public class SchedulerService(
    IAllocationRepository _allocationRepository,
    IUserRepository _userRepository,
    IProjectRepository _projectRepository,
    IProjectRiskFlagRepository _projectRiskFlagRepository,
    IProjectHealthService _projectHealthService,
    ILogger<SchedulerService> _logger) : ISchedulerService
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduler job started.");

        await UpdateResourcesStatus(cancellationToken);
        await ComputeProjectHealth(cancellationToken);

        _logger.LogInformation("Scheduler job completed.");
    }

    private async Task UpdateResourcesStatus(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetResourceUsers(
            new ResourceFilter { IncludeInactive = false },
            cancellationToken);

        foreach (var user in users)
        {
            await ApplyStatus(user, cancellationToken);
        }

        await _userRepository.SaveChanges(cancellationToken);
        _logger.LogInformation("Updated bench status for {ResourceCount} resources.", users.Count);
    }

    private async Task ApplyStatus(
        User user,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = user.Id,
                FromDate = today,
                ToDate = today,
            },
            cancellationToken);

        var status = utilization > 0
            ? ResourceStatusTypeEnum.Allocated
            : ResourceStatusTypeEnum.Bench;

        await _userRepository.SetCurrentResourceStatus(user.Id, (int)status, cancellationToken);
    }

    private async Task ComputeProjectHealth(CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetAllWithManager(cancellationToken);
        var updatedCount = 0;

        foreach (var project in projects)
        {
            var projectDetails = await _projectRepository.GetByIdWithDetails(project.Id, cancellationToken);
            if (projectDetails is null)
            {
                continue;
            }

            var evaluation = await _projectHealthService.Evaluate(projectDetails, cancellationToken);
            projectDetails.HealthStatus = evaluation.HealthStatus;
            _projectRepository.Update(projectDetails);

            await _projectRiskFlagRepository.ReplaceForProject(
                projectDetails.Id,
                ToEntities(evaluation.RiskFlags),
                cancellationToken);

            updatedCount++;
        }

        await _projectRepository.SaveChanges(cancellationToken);
        _logger.LogInformation(
            "Updated health and risk flags for {UpdatedCount} of {ProjectCount} projects.",
            updatedCount,
            projects.Count);
    }

    private static IReadOnlyList<ProjectRiskFlag> ToEntities(IReadOnlyList<RiskFlagItem> riskFlags)
    {
        return riskFlags
            .Select((flag, index) => new ProjectRiskFlag
            {
                SortOrder = index + 1,
                Outcome = flag.Outcome,
                Message = flag.Message,
            })
            .ToList();
    }
}
