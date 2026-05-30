namespace Prm.Data.Entities;

public class SystemConfiguration : BaseEntity
{
    public int Id { get; set; }
    public required string Provider { get; set; }
    public required string ApiKey { get; set; }
    public int SchedulerInterval { get; set; }
    public int MaxWeeklyHours { get; set; }
}
