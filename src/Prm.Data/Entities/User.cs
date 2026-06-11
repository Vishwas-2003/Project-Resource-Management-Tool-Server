namespace Prm.Data.Entities;

public class User : BaseEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public required string FullName { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public required string Department { get; set; }
    public required string Designation { get; set; }
    public DateTime? PasswordExpiryTime { get; set; }
    public Role Role { get; set; } = null!;
    public RefreshToken? RefreshToken { get; set; }
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    public ICollection<ResourceManagerHistory> ManagerHistories { get; set; } = new List<ResourceManagerHistory>();
    public ICollection<ResourceManagerHistory> SubordinateManagerHistories { get; set; } = new List<ResourceManagerHistory>();
    public ICollection<ResourceStatusHistory> ResourceStatusHistories { get; set; } = new List<ResourceStatusHistory>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
