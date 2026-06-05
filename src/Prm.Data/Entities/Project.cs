namespace Prm.Data.Entities;

public class Project : BaseEntity
{
    public int Id { get; set; }
    public int ManagerUserId { get; set; }
    public int TotalStoryPoints { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public required string Status { get; set; }
    public User ManagerUser { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
}
