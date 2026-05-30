using Prm.Common.Models.Auth;

namespace UserManagement.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
