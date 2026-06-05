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
    public bool ForcePasswordChange { get; set; }
    public Role Role { get; set; } = null!;
    public RefreshToken? RefreshToken { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<Employee> ManagedEmployees { get; set; } = new List<Employee>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
}
