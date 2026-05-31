namespace Prm.Common.Models.Milestones;

public class ProjectMilestonesResult
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public IReadOnlyList<MilestoneSummary> Milestones { get; set; } = [];
}
