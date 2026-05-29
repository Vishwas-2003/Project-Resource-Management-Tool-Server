using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
