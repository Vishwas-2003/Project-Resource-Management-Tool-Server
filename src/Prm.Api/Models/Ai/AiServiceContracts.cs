using System.Text.Json.Serialization;

namespace Prm.Api.Models.Ai;

public sealed class RiskSummaryRequest
{
    [JsonPropertyName("project_id")]
    public int ProjectId { get; init; }
}

public sealed class RiskSummaryResponse
{
    [JsonPropertyName("project_id")]
    public int ProjectId { get; init; }
    [JsonPropertyName("project_name")]
    public string ProjectName { get; init; } = string.Empty;
    [JsonPropertyName("health_status")]
    public string HealthStatus { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Disclaimer { get; init; } = string.Empty;
}

public sealed class TeamBuilderRequest
{
    public string Query { get; init; } = string.Empty;
}

public sealed class TeamBuilderResponse
{
    public string Query { get; init; } = string.Empty;
    public IReadOnlyList<TeamBuilderMember> Team { get; init; } = [];
    public IReadOnlyList<TeamBuilderUnavailableItem> Unavailable { get; init; } = [];
    public string? Summary { get; init; }
    public string Disclaimer { get; init; } = string.Empty;
    public string? Message { get; init; }
}

public sealed class TeamBuilderMember
{
    public string Role { get; init; } = string.Empty;
    public int ResourceUserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SkillsMatch { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public sealed class TeamBuilderUnavailableItem
{
    public string? Role { get; init; }
    public int? ResourceUserId { get; init; }
    public string? Name { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string UnavailableReason { get; init; } = string.Empty;
}
