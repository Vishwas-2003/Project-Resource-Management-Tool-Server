namespace Prm.Data.Entities;

public class Milestone : BaseEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public required string Title { get; set; }
    public DateOnly DueDate { get; set; }
    public int StoryPoints { get; set; }
    public required string Status { get; set; }
    public Project Project { get; set; } = null!;
}
