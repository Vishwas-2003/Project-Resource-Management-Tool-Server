namespace Prm.Data.Entities;

public class Employee : BaseEntity
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public required string Department { get; set; }
    public required string Designation { get; set; }
    public required string Status { get; set; }
    public User User { get; set; } = null!;
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
}
