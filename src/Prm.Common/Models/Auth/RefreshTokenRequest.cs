using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
