namespace Prm.Data.Entities;

public class Allocation : BaseEntity
{
    public int AllocationId { get; set; }
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public Employee Employee { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
