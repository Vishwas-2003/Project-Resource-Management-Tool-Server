namespace Prm.Data.Entities;

public class TimesheetEntry : BaseEntity
{
    public int EntryId { get; set; }
    public int TimesheetId { get; set; }
    public int ProjectId { get; set; }
    public int HoursWorked { get; set; }
    public Timesheet Timesheet { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<TimesheetActivityTag> ActivityTags { get; set; } = new List<TimesheetActivityTag>();
}
