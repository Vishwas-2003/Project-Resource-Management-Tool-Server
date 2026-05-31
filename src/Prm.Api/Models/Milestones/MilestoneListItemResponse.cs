namespace Prm.Api.Models.Milestones;

public class MilestoneListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
