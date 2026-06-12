using System.Text;
using Prm.Api.Models.Ai;
using Prm.Api.Models.Email;
using Prm.Common.Constants;
using Prm.Data.Entities;

namespace Prm.Api.Services;

internal static class ProjectRiskAlertEmailBuilder
{
    public static EmailMessage Build(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        IReadOnlyList<Milestone> keyMilestones,
        string riskSummary,
        TeamBuilderResponse? teamSuggestions)
    {
        var managerName = project.ManagerUser.FullName;
        var subject = string.Format(AppConstants.Email.RiskAlertSubject, project.Name);
        var bodyHtml = BuildBodyHtml(project, riskFlags, keyMilestones, riskSummary, teamSuggestions);
        var html = EmailLayoutBuilder.BuildHtml(
            AppConstants.Email.RiskAlertTitle,
            EmailLayoutBuilder.AccentDanger,
            bodyHtml,
            AppConstants.Email.AiDisclaimer);
        var text = EmailLayoutBuilder.BuildText(
            BuildBodyText(project, riskFlags, keyMilestones, riskSummary, teamSuggestions),
            AppConstants.Email.AiDisclaimer);

        return new EmailMessage
        {
            ToEmail = project.ManagerUser.Email,
            ToName = managerName,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
    }

    private static string BuildBodyHtml(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        IReadOnlyList<Milestone> keyMilestones,
        string riskSummary,
        TeamBuilderResponse? teamSuggestions)
    {
        var builder = new StringBuilder();
        builder.Append(EmailLayoutBuilder.Paragraph(AppConstants.Email.RiskAlertIntro));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionProjectDetails));
        builder.Append(BuildProjectDetailsHtml(project));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionHealthStatus));
        builder.Append(EmailLayoutBuilder.Paragraph(
            $"<strong>{EmailLayoutBuilder.Encode(FormatHealthStatus(project.HealthStatus))}</strong>"));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionKeyMilestones));
        builder.Append(BuildMilestonesHtml(keyMilestones));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionRiskFlags));
        builder.Append(BuildRiskFlagsHtml(riskFlags));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionAiRiskSummary));
        builder.Append(EmailLayoutBuilder.Paragraph(EmailLayoutBuilder.Encode(riskSummary)));
        builder.Append(EmailLayoutBuilder.SectionHeading(AppConstants.Email.SectionSuggestedHelp));
        builder.Append(BuildTeamSuggestionsHtml(teamSuggestions));
        return builder.ToString();
    }

    private static string BuildBodyText(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        IReadOnlyList<Milestone> keyMilestones,
        string riskSummary,
        TeamBuilderResponse? teamSuggestions)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AppConstants.Email.RiskAlertTitle);
        builder.AppendLine();
        builder.AppendLine(AppConstants.Email.SectionProjectDetails);
        builder.AppendLine($"{AppConstants.Email.LabelName}: {project.Name}");
        builder.AppendLine($"{AppConstants.Email.LabelManager}: {project.ManagerUser.FullName}");
        builder.AppendLine($"{AppConstants.Email.LabelStatus}: {project.Status}");
        builder.AppendLine($"{AppConstants.Email.LabelPeriod}: {project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine($"{AppConstants.Email.SectionHealthStatus}: {FormatHealthStatus(project.HealthStatus)}");
        builder.AppendLine();
        builder.AppendLine(AppConstants.Email.SectionKeyMilestones);
        builder.AppendLine(BuildMilestonesText(keyMilestones));
        builder.AppendLine();
        builder.AppendLine(AppConstants.Email.SectionRiskFlags);
        builder.AppendLine(BuildRiskFlagsText(riskFlags));
        builder.AppendLine();
        builder.AppendLine(AppConstants.Email.SectionAiRiskSummary);
        builder.AppendLine(riskSummary);
        builder.AppendLine();
        builder.AppendLine(AppConstants.Email.SectionSuggestedHelp);
        builder.AppendLine(BuildTeamSuggestionsText(teamSuggestions));
        return builder.ToString().TrimEnd();
    }

    private static string BuildProjectDetailsHtml(Project project) =>
        $"""
        <ul style="margin:0 0 16px;padding-left:20px;">
          <li><strong>{AppConstants.Email.LabelName}:</strong> {EmailLayoutBuilder.Encode(project.Name)}</li>
          <li><strong>{AppConstants.Email.LabelManager}:</strong> {EmailLayoutBuilder.Encode(project.ManagerUser.FullName)}</li>
          <li><strong>{AppConstants.Email.LabelStatus}:</strong> {EmailLayoutBuilder.Encode(project.Status)}</li>
          <li><strong>{AppConstants.Email.LabelPeriod}:</strong> {project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}</li>
        </ul>
        """;

    private static string BuildMilestonesHtml(IReadOnlyList<Milestone> milestones)
    {
        if (milestones.Count == 0)
        {
            return EmailLayoutBuilder.Paragraph(AppConstants.Email.NoOpenMilestones);
        }

        var items = milestones
            .Select(milestone =>
            {
                var overdue = milestone.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                    && milestone.Status != MilestoneConstants.StatusDone;
                var suffix = overdue ? AppConstants.Email.MilestoneOverdueSuffix : string.Empty;
                return $"<li>{EmailLayoutBuilder.Encode(milestone.Title)} — due {milestone.DueDate:yyyy-MM-dd}, {EmailLayoutBuilder.Encode(milestone.Status)}{suffix}</li>";
            });

        return $"<ul style=\"margin:0 0 16px;padding-left:20px;\">{string.Join(string.Empty, items)}</ul>";
    }

    private static string BuildMilestonesText(IReadOnlyList<Milestone> milestones)
    {
        if (milestones.Count == 0)
        {
            return AppConstants.Email.NoOpenMilestones;
        }

        return string.Join(
            Environment.NewLine,
            milestones.Select(milestone =>
            {
                var overdue = milestone.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                    && milestone.Status != MilestoneConstants.StatusDone;
                var suffix = overdue ? AppConstants.Email.MilestoneOverdueSuffix : string.Empty;
                return $"- {milestone.Title} — due {milestone.DueDate:yyyy-MM-dd}, {milestone.Status}{suffix}";
            }));
    }

    private static string BuildRiskFlagsHtml(IReadOnlyList<ProjectRiskFlag> riskFlags)
    {
        if (riskFlags.Count == 0)
        {
            return EmailLayoutBuilder.Paragraph(AppConstants.Email.NoRiskFlags);
        }

        var items = riskFlags
            .OrderBy(flag => flag.SortOrder)
            .Select(flag =>
                $"<li><strong>{EmailLayoutBuilder.Encode(flag.Outcome)}:</strong> {EmailLayoutBuilder.Encode(flag.Message)}</li>");

        return $"<ul style=\"margin:0 0 16px;padding-left:20px;\">{string.Join(string.Empty, items)}</ul>";
    }

    private static string BuildRiskFlagsText(IReadOnlyList<ProjectRiskFlag> riskFlags)
    {
        if (riskFlags.Count == 0)
        {
            return AppConstants.Email.NoRiskFlags;
        }

        return string.Join(
            Environment.NewLine,
            riskFlags
                .OrderBy(flag => flag.SortOrder)
                .Select(flag => $"- {flag.Outcome}: {flag.Message}"));
    }

    private static string BuildTeamSuggestionsHtml(TeamBuilderResponse? teamSuggestions)
    {
        if (teamSuggestions is null)
        {
            return EmailLayoutBuilder.Paragraph(EmailLayoutBuilder.Encode(AppConstants.Email.AiTeamSuggestionsUnavailable));
        }

        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(teamSuggestions.Summary))
        {
            builder.Append(EmailLayoutBuilder.Paragraph(EmailLayoutBuilder.Encode(teamSuggestions.Summary)));
        }
        else if (!string.IsNullOrWhiteSpace(teamSuggestions.Message))
        {
            builder.Append(EmailLayoutBuilder.Paragraph(EmailLayoutBuilder.Encode(teamSuggestions.Message)));
        }

        if (teamSuggestions.Team.Count > 0)
        {
            builder.Append(EmailLayoutBuilder.Paragraph(
                $"<strong>{AppConstants.Email.RecommendedBenchAllocations}</strong>"));
            builder.Append("<ul style=\"margin:0 0 16px;padding-left:20px;\">");
            foreach (var member in teamSuggestions.Team)
            {
                builder.Append("<li>");
                builder.Append($"<strong>{EmailLayoutBuilder.Encode(member.Name)}</strong> as {EmailLayoutBuilder.Encode(member.Role)} — ");
                builder.Append($"{EmailLayoutBuilder.Encode(member.SkillsMatch)}. {EmailLayoutBuilder.Encode(member.Reason)}");
                builder.Append("</li>");
            }

            builder.Append("</ul>");
        }
        else
        {
            builder.Append(EmailLayoutBuilder.Paragraph(AppConstants.Email.NoBenchResourcesSuggested));
        }

        return builder.ToString();
    }

    private static string BuildTeamSuggestionsText(TeamBuilderResponse? teamSuggestions)
    {
        if (teamSuggestions is null)
        {
            return AppConstants.Email.AiTeamSuggestionsUnavailable;
        }

        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(teamSuggestions.Summary))
        {
            builder.AppendLine(teamSuggestions.Summary);
        }
        else if (!string.IsNullOrWhiteSpace(teamSuggestions.Message))
        {
            builder.AppendLine(teamSuggestions.Message);
        }

        if (teamSuggestions.Team.Count == 0)
        {
            builder.AppendLine(AppConstants.Email.NoBenchResourcesSuggested);
            return builder.ToString().TrimEnd();
        }

        foreach (var member in teamSuggestions.Team)
        {
            builder.AppendLine(
                $"- {member.Name} as {member.Role}: {member.SkillsMatch}. {member.Reason}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatHealthStatus(string healthStatus) =>
        healthStatus switch
        {
            ManagerConstants.HealthAtRisk => AppConstants.Email.HealthStatusAtRisk,
            ManagerConstants.HealthAttention => AppConstants.Email.HealthStatusNeedsAttention,
            ManagerConstants.HealthOnTrack => AppConstants.Email.HealthStatusOnTrack,
            _ => healthStatus,
        };
}
