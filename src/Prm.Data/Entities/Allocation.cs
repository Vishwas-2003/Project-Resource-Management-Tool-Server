namespace Prm.Data.Entities;

public class Allocation : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
