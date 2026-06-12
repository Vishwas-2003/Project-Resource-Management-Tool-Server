using Prm.Common.Models.Manager;
using Prm.Data.Entities;

namespace Prm.Api.Services;

public partial class SchedulerService
{
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
