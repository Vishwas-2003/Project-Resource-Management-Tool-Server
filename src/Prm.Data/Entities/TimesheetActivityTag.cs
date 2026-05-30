namespace Prm.Data.Entities;

public class TimesheetActivityTag : BaseEntity
{
    public int Id { get; set; }
    public int TimesheetEntryId { get; set; }
    public int ActivityTagId { get; set; }
    public TimesheetEntry TimesheetEntry { get; set; } = null!;
    public ActivityTag ActivityTag { get; set; } = null!;
}
