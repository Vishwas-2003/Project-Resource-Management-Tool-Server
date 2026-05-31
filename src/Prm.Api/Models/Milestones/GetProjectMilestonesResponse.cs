namespace Prm.Api.Models.Milestones;

public class GetProjectMilestonesResponse
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public IReadOnlyList<MilestoneListItemResponse> Milestones { get; set; } = [];
}
