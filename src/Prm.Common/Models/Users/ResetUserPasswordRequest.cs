using System.ComponentModel.DataAnnotations;

namespace Prm.Common.Models.Users;

public class ResetUserPasswordRequest : UserLookupRequest
{
    [Required]
    [MinLength(8)]
    public string TemporaryPassword { get; set; } = string.Empty;
}
