using Prm.Common.Models.Auth;
using Prm.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshTokenValue) GenerateTokens(User user);
}
