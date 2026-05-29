namespace Prm.Common.Models.Auth;

public class AuthResponse
{
    public AuthenticatedUser User { get; set; } = new();
    public AuthTokens Tokens { get; set; } = new();
}
