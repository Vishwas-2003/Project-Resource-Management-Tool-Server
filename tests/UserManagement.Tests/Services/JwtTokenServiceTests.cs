using System.IdentityModel.Tokens.Jwt;
using UserManagement.Configuration;
using UserManagement.Services;
using UserManagement.Tests.Helpers;

namespace UserManagement.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(TestData.CreateJwtOptionsAccessor());
    private readonly JwtOptions _jwtOptions = TestData.CreateJwtOptions();

    [Fact]
    public void GenerateTokens_ReturnsNonEmptyAccessAndRefreshTokens()
    {
        var user = TestData.CreateUser();

        var (accessToken, _, refreshToken) = _sut.GenerateTokens(user);

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
    }

    [Fact]
    public void GenerateTokens_AccessTokenContainsExpectedClaims()
    {
        var user = TestData.CreateUser();

        var (accessToken, _, _) = _sut.GenerateTokens(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        Assert.Equal(_jwtOptions.Issuer, jwt.Issuer);
        Assert.Contains(jwt.Audiences, audience => audience == _jwtOptions.Audience);
        Assert.Equal(user.UserId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Username, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Contains(jwt.Claims, c => c.Value == user.Role.Name);
    }

    [Fact]
    public void GenerateTokens_AccessTokenExpiryMatchesConfiguredMinutes()
    {
        var user = TestData.CreateUser();
        var before = DateTime.UtcNow;

        var (_, expiresAtUtc, _) = _sut.GenerateTokens(user);

        var expectedMinimum = before.AddMinutes(_jwtOptions.AccessTokenMinutes).AddSeconds(-2);
        var expectedMaximum = before.AddMinutes(_jwtOptions.AccessTokenMinutes).AddSeconds(2);
        Assert.InRange(expiresAtUtc, expectedMinimum, expectedMaximum);
    }

    [Fact]
    public void GenerateTokens_RefreshTokensAreUniquePerCall()
    {
        var user = TestData.CreateUser();

        var first = _sut.GenerateTokens(user).RefreshTokenValue;
        var second = _sut.GenerateTokens(user).RefreshTokenValue;

        Assert.NotEqual(first, second);
    }
}
