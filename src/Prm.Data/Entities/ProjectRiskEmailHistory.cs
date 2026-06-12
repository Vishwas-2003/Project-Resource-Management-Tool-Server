namespace Prm.Data.Entities;

public class ProjectRiskEmailHistory : BaseEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int ManagerUserId { get; set; }
    public DateOnly SentOnDate { get; set; }
    public DateTime SentAtUtc { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public Project Project { get; set; } = null!;
    public User ManagerUser { get; set; } = null!;
}
