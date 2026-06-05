namespace Prm.Common.Models.Projects;

public class ProjectSummary
{
    public int RowNumber { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int StoryPointsDone { get; set; }
    public int TotalStoryPoints { get; set; }
}
