namespace Prm.Common.Models.Manager;

public class ProjectHealthEvaluation
{
    public string HealthStatus { get; set; } = string.Empty;
    public IReadOnlyList<RiskFlagItem> RiskFlags { get; set; } = [];
}
