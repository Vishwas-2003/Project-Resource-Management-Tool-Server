namespace Prm.Data.Entities;

public class ProjectRiskFlag : BaseEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int SortOrder { get; set; }
    public required string Outcome { get; set; }
    public required string Message { get; set; }
    public Project Project { get; set; } = null!;
}
