namespace Prm.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public required string FullName { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ForcePasswordChange { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Role Role { get; set; } = null!;
    public RefreshToken? RefreshToken { get; set; }
}
