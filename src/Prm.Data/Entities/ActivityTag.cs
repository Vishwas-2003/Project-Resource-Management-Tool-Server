namespace Prm.Data.Entities;

public class ActivityTag : BaseEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<TimesheetActivityTag> TimesheetActivityTags { get; set; } = new List<TimesheetActivityTag>();
}
