namespace Prm.Data.Entities;

public class EmailNotificationHistory : BaseEntity
{
    public int Id { get; set; }
    public int EmailTypeId { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Related entity id: <see cref="Project.Id"/> for project risk alerts,
    /// or <see cref="Timesheet.Id"/> for missed timesheet notifications.
    /// </summary>
    public int? EntityId { get; set; }
    public DateOnly SentOnDate { get; set; }
    public DateTime SentAtUtc { get; set; }
    public required string RecipientEmail { get; set; }
    public required string Subject { get; set; }
    public User User { get; set; } = null!;
}
