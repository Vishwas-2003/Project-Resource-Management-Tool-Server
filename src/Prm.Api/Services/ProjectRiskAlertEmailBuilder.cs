using System.Net;
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
        var html = BuildHtml(project, riskFlags, keyMilestones, riskSummary, teamSuggestions);
        var text = BuildText(project, riskFlags, keyMilestones, riskSummary, teamSuggestions);

        return new EmailMessage
        {
            ToEmail = project.ManagerUser.Email,
            ToName = managerName,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
    }

    private static string BuildHtml(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        IReadOnlyList<Milestone> keyMilestones,
        string riskSummary,
        TeamBuilderResponse? teamSuggestions)
    {
        var builder = new StringBuilder();
        builder.Append("<html><body style=\"font-family:Segoe UI,Arial,sans-serif;color:#222;\">");
        builder.Append("<h2>Project At Risk Alert</h2>");
        builder.Append("<p>The following project under your management has been flagged as <strong>At Risk</strong>.</p>");

        AppendSection(builder, "Project Details", BuildProjectDetailsHtml(project));
        AppendSection(builder, "Health Status", $"<p><strong>{FormatHealthStatus(project.HealthStatus)}</strong></p>");
        AppendSection(builder, "Key Milestones", BuildMilestonesHtml(keyMilestones));
        AppendSection(builder, "Risk Flags", BuildRiskFlagsHtml(riskFlags));
        AppendSection(builder, "AI Risk Summary", $"<p>{Encode(riskSummary)}</p>");
        AppendSection(builder, "Suggested Help", BuildTeamSuggestionsHtml(teamSuggestions));

        builder.Append("<p style=\"color:#666;font-size:12px;\">");
        builder.Append("This is an automated notification from the PRM system. ");
        builder.Append("AI-generated sections should be verified before making allocation decisions.");
        builder.Append("</p>");
        builder.Append("</body></html>");
        return builder.ToString();
    }

    private static string BuildText(
        Project project,
        IReadOnlyList<ProjectRiskFlag> riskFlags,
        IReadOnlyList<Milestone> keyMilestones,
        string riskSummary,
        TeamBuilderResponse? teamSuggestions)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Project At Risk Alert");
        builder.AppendLine();
        builder.AppendLine("Project Details");
        builder.AppendLine($"Name: {project.Name}");
        builder.AppendLine($"Manager: {project.ManagerUser.FullName}");
        builder.AppendLine($"Status: {project.Status}");
        builder.AppendLine($"Period: {project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine($"Health Status: {FormatHealthStatus(project.HealthStatus)}");
        builder.AppendLine();
        builder.AppendLine("Key Milestones");
        builder.AppendLine(BuildMilestonesText(keyMilestones));
        builder.AppendLine();
        builder.AppendLine("Risk Flags");
        builder.AppendLine(BuildRiskFlagsText(riskFlags));
        builder.AppendLine();
        builder.AppendLine("AI Risk Summary");
        builder.AppendLine(riskSummary);
        builder.AppendLine();
        builder.AppendLine("Suggested Help");
        builder.AppendLine(BuildTeamSuggestionsText(teamSuggestions));
        return builder.ToString();
    }

    private static void AppendSection(StringBuilder builder, string title, string content)
    {
        builder.Append("<h3 style=\"margin-top:24px;\">");
        builder.Append(Encode(title));
        builder.Append("</h3>");
        builder.Append(content);
    }

    private static string BuildProjectDetailsHtml(Project project) =>
        $"""
        <ul>
          <li><strong>Name:</strong> {Encode(project.Name)}</li>
          <li><strong>Manager:</strong> {Encode(project.ManagerUser.FullName)}</li>
          <li><strong>Status:</strong> {Encode(project.Status)}</li>
          <li><strong>Period:</strong> {project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}</li>
        </ul>
        """;

    private static string BuildMilestonesHtml(IReadOnlyList<Milestone> milestones)
    {
        if (milestones.Count == 0)
        {
            return "<p>No open milestones found.</p>";
        }

        var items = milestones
            .Select(milestone =>
            {
                var overdue = milestone.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                    && milestone.Status != MilestoneConstants.StatusDone;
                var suffix = overdue ? " (overdue)" : string.Empty;
                return $"<li>{Encode(milestone.Title)} — due {milestone.DueDate:yyyy-MM-dd}, {Encode(milestone.Status)}{suffix}</li>";
            });

        return $"<ul>{string.Join(string.Empty, items)}</ul>";
    }

    private static string BuildMilestonesText(IReadOnlyList<Milestone> milestones)
    {
        if (milestones.Count == 0)
        {
            return "No open milestones found.";
        }

        return string.Join(
            Environment.NewLine,
            milestones.Select(milestone =>
            {
                var overdue = milestone.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)
                    && milestone.Status != MilestoneConstants.StatusDone;
                var suffix = overdue ? " (overdue)" : string.Empty;
                return $"- {milestone.Title} — due {milestone.DueDate:yyyy-MM-dd}, {milestone.Status}{suffix}";
            }));
    }

    private static string BuildRiskFlagsHtml(IReadOnlyList<ProjectRiskFlag> riskFlags)
    {
        if (riskFlags.Count == 0)
        {
            return "<p>No risk flags recorded.</p>";
        }

        var items = riskFlags
            .OrderBy(flag => flag.SortOrder)
            .Select(flag => $"<li><strong>{Encode(flag.Outcome)}:</strong> {Encode(flag.Message)}</li>");

        return $"<ul>{string.Join(string.Empty, items)}</ul>";
    }

    private static string BuildRiskFlagsText(IReadOnlyList<ProjectRiskFlag> riskFlags)
    {
        if (riskFlags.Count == 0)
        {
            return "No risk flags recorded.";
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
            return $"<p>{Encode(AppConstants.Email.AiTeamSuggestionsUnavailable)}</p>";
        }

        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(teamSuggestions.Summary))
        {
            builder.Append($"<p>{Encode(teamSuggestions.Summary)}</p>");
        }
        else if (!string.IsNullOrWhiteSpace(teamSuggestions.Message))
        {
            builder.Append($"<p>{Encode(teamSuggestions.Message)}</p>");
        }

        if (teamSuggestions.Team.Count > 0)
        {
            builder.Append("<p><strong>Recommended bench allocations:</strong></p><ul>");
            foreach (var member in teamSuggestions.Team)
            {
                builder.Append("<li>");
                builder.Append($"<strong>{Encode(member.Name)}</strong> as {Encode(member.Role)} — ");
                builder.Append($"{Encode(member.SkillsMatch)}. {Encode(member.Reason)}");
                builder.Append("</li>");
            }

            builder.Append("</ul>");
        }
        else
        {
            builder.Append("<p>No bench resources were suggested for this project.</p>");
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
            builder.AppendLine("No bench resources were suggested for this project.");
            return builder.ToString();
        }

        foreach (var member in teamSuggestions.Team)
        {
            builder.AppendLine(
                $"- {member.Name} as {member.Role}: {member.SkillsMatch}. {member.Reason}");
        }

        return builder.ToString();
    }

    private static string FormatHealthStatus(string healthStatus) =>
        healthStatus switch
        {
            ManagerConstants.HealthAtRisk => "At Risk",
            ManagerConstants.HealthAttention => "Needs Attention",
            ManagerConstants.HealthOnTrack => "On Track",
            _ => healthStatus,
        };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
