using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Users;

public class CreateUserRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Range(1, 3)]
    public int RoleId { get; set; }
}
