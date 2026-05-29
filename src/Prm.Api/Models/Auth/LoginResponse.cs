namespace Prm.Api.Models.Auth;

public class LoginResponse
{
    public UserResponse User { get; set; } = new();
    public TokenResponse Tokens { get; set; } = new();
}
