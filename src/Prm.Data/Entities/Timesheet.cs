namespace Prm.Data.Entities;

public class Timesheet : BaseEntity
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WeekStart { get; set; }
    public int TotalHours { get; set; }
    public required string Status { get; set; }
    public Employee Employee { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
