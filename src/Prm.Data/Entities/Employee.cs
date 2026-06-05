namespace Prm.Data.Entities;

public class Employee : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ManagerUserId { get; set; }
    public required string Department { get; set; }
    public required string Designation { get; set; }
    public string? Status { get; set; }
    public User User { get; set; } = null!;
    public User? ManagerUser { get; set; }
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
