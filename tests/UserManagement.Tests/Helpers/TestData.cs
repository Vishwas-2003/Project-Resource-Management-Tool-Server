using Microsoft.Extensions.Options;
using Prm.Data.Entities;
using UserManagement.Configuration;

namespace UserManagement.Tests.Helpers;

internal static class TestData
{
    internal const string Password = "Admin@1234";
    internal const string Username = "admin";

    internal static JwtOptions CreateJwtOptions() =>
        new()
        {
            Issuer = "Prm.Test",
            Audience = "Prm.Test.Client",
            Secret = "9f3a7c1d4b8e2a6c5d0f1e3b7a9c4d2e8f1a3c5e7b9d0f2a4c6e8b0d2f4a6c8",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7,
        };

    internal static IOptions<JwtOptions> CreateJwtOptionsAccessor() =>
        Options.Create(CreateJwtOptions());

    internal static User CreateUser(bool isActive = true, string username = Username)
    {
        var user = new User
        {
            UserId = 1,
            RoleId = 1,
            FullName = "System Administrator",
            Username = username,
            Email = "admin@prm.local",
            PasswordHash = string.Empty,
            IsActive = isActive,
            ForcePasswordChange = false,
            CreatedAtUtc = DateTime.UtcNow,
            Role = new Role { RoleId = 1, Name = "Admin" },
        };

        return user;
    }

    internal static RefreshToken CreateRefreshToken(User user, string token = "existing-refresh-token", DateTime? expiryDateUtc = null) =>
        new()
        {
            RefreshTokenId = 10,
            UserId = user.UserId,
            Token = token,
            ExpiryDateUtc = expiryDateUtc ?? DateTime.UtcNow.AddDays(1),
            User = user,
        };
}
