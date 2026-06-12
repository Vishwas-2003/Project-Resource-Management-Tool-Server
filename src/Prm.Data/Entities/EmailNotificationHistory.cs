namespace Prm.Data.Entities;

public class EmailNotificationHistory : BaseEntity
{
    public int Id { get; set; }
    public int EmailTypeId { get; set; }
    public int UserId { get; set; }
    public int? ProjectId { get; set; }
    public DateOnly SentOnDate { get; set; }
    public DateTime SentAtUtc { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public User User { get; set; } = null!;
    public Project? Project { get; set; }
}
