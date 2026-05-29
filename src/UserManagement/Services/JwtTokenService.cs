using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prm.Data.Entities;
using UserManagement.Configuration;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services;

public class JwtTokenService(IOptions<JwtOptions> _jwtOptionsAccessor) : IJwtTokenService
{
    public (string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshTokenValue) GenerateTokens(User user)
    {
        var jwtOptions = _jwtOptionsAccessor.Value;
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var accessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

        return (accessToken, expiresAtUtc, GenerateRefreshTokenValue());
    }

    private static string GenerateRefreshTokenValue()
    {
        Span<byte> randomBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
