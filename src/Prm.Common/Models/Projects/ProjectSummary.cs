namespace Prm.Common.Models.Projects;

public class ProjectSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
