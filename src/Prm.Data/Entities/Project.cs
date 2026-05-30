namespace Prm.Data.Entities;

public class Project : BaseEntity
{
    public int ProjectId { get; set; }
    public int ManagerEmployeeId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public required string Status { get; set; }
    public Employee ManagerEmployee { get; set; } = null!;
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();
}
