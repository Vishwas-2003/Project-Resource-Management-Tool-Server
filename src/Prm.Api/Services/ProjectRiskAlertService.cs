using Prm.Api.Models.Ai;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class ProjectRiskAlertService(
    IProjectRepository projectRepository,
    IProjectRiskFlagRepository projectRiskFlagRepository,
    IEmailNotificationHistoryRepository emailNotificationHistoryRepository,
    IAiServiceClient aiServiceClient,
    IEmailNotificationService emailNotificationService,
    ILogger<ProjectRiskAlertService> logger) : IProjectRiskAlertService
{
    private const int MaxKeyMilestones = 5;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Project risk alert job started.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var projects = await projectRepository.GetAllWithManager(cancellationToken);
        var atRiskProjects = projects
            .Where(project => project.HealthStatus == ManagerConstants.HealthAtRisk)
            .ToList();

        var emailsSent = 0;
        var skippedAlreadySent = 0;

        foreach (var project in atRiskProjects)
        {
            if (!CanNotifyManager(project))
            {
                logger.LogWarning(
                    "Skipping risk alert for project {ProjectId} because the manager is inactive or has no email.",
                    project.Id);
                continue;
            }

            if (await emailNotificationHistoryRepository.ExistsForProjectRiskOnDateAsync(
                    project.Id,
                    today,
                    cancellationToken))
            {
                skippedAlreadySent++;
                logger.LogInformation(
                    "Skipping risk alert for project {ProjectId} because an email was already sent on {SentOnDate}.",
                    project.Id,
                    today);
                continue;
            }

            try
            {
                await SendAlertAsync(project, today, cancellationToken);
                emailsSent++;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send project risk alert for project {ProjectId}.",
                    project.Id);
            }
        }

        logger.LogInformation(
            "Project risk alert job completed. Sent {EmailsSent}, skipped {SkippedAlreadySent}, of {AtRiskCount} at-risk projects.",
            emailsSent,
            skippedAlreadySent,
            atRiskProjects.Count);
    }

    private async Task SendAlertAsync(
        Project project,
        DateOnly sentOnDate,
        CancellationToken cancellationToken)
    {
        var riskFlags = await projectRiskFlagRepository.GetByProjectId(project.Id, cancellationToken);
        var keyMilestones = GetKeyMilestones(project.Milestones);
        var riskSummary = await GetRiskSummaryAsync(project.Id, cancellationToken);
        var teamSuggestions = await GetTeamSuggestionsAsync(project, riskFlags, cancellationToken);
        var email = ProjectRiskAlertEmailBuilder.Build(
            project,
            riskFlags,
            keyMilestones,
            riskSummary,
            teamSuggestions);

        await emailNotificationService.SendAsync(email, cancellationToken);

        var sentAtUtc = DateTime.UtcNow;
        await emailNotificationHistoryRepository.Add(
            new EmailNotificationHistory
            {
                EmailTypeId = (int)EmailNotificationTypeEnum.ProjectRisk,
                UserId = project.ManagerUserId,
                ProjectId = project.Id,
                SentOnDate = sentOnDate,
                SentAtUtc = sentAtUtc,
                RecipientEmail = project.ManagerUser.Email,
                Subject = email.Subject,
            },
            cancellationToken);
        await emailNotificationHistoryRepository.SaveChanges(cancellationToken);

        logger.LogInformation(
            "Logged project risk email history for project {ProjectId}, user {UserId}, sent on {SentOnDate}.",
            project.Id,
            project.ManagerUserId,
            sentOnDate);
    }

    private async Task<string> GetRiskSummaryAsync(int projectId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await aiServiceClient.GetRiskSummaryAsync(projectId, cancellationToken);
            return string.IsNullOrWhiteSpace(response.Summary)
                ? AppConstants.Email.AiRiskSummaryUnavailable
                : response.Summary.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "AiService risk summary failed for project {ProjectId}.",
                projectId);
            return AppConstants.Email.AiRiskSummaryUnavailable;
        }
    }

    private async Task<TeamBuilderResponse?> GetTeamSuggestionsAsync(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = BuildTeamSuggestionQuery(project, riskFlags);
            return await aiServiceClient.BuildTeamAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "AiService team suggestions failed for project {ProjectId}.",
                project.Id);
            return null;
        }
    }

    private static bool CanNotifyManager(Project project) =>
        project.ManagerUser is { IsActive: true }
        && !string.IsNullOrWhiteSpace(project.ManagerUser.Email);

    private static IReadOnlyList<Milestone> GetKeyMilestones(IEnumerable<Milestone> milestones)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var openMilestones = milestones
            .Where(milestone => milestone.Status != MilestoneConstants.StatusDone)
            .OrderBy(milestone => milestone.DueDate)
            .ToList();

        var overdue = openMilestones.Where(milestone => milestone.DueDate < today);
        var upcoming = openMilestones.Where(milestone => milestone.DueDate >= today).Take(3);

        return overdue
            .Concat(upcoming)
            .DistinctBy(milestone => milestone.Id)
            .Take(MaxKeyMilestones)
            .ToList();
    }

    private static string BuildTeamSuggestionQuery(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags)
    {
        var issues = riskFlags
            .Where(flag => flag.Outcome == ManagerConstants.RiskFlagFail)
            .Select(flag => flag.Message)
            .ToList();

        if (issues.Count == 0)
        {
            issues = riskFlags.Select(flag => flag.Message).ToList();
        }

        var issueText = issues.Count == 0
            ? "delivery and resource risks"
            : string.Join("; ", issues);

        return
            $"Suggest bench resources who could reduce risk for project \"{project.Name}\". "
            + $"Current issues: {issueText}. "
            + "Recommend roles and employees available on bench who would help recover the project if allocated.";
    }
}
