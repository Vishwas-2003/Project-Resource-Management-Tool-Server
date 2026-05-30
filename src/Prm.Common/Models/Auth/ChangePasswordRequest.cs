using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Auth;

public class ChangePasswordRequest
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
