using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Prm.Common.Constants;
using Prm.Common.Models.Auth;
using Prm.Data.Entities;
using Prm.Data.Profiles;
using Prm.Data.Repositories.Interfaces;
using UserManagement.Configuration;
using UserManagement.Services;
using UserManagement.Services.Interfaces;
using UserManagement.Tests.Helpers;

namespace UserManagement.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IMapper _mapper;
    private readonly IOptions<JwtOptions> _jwtOptions = TestData.CreateJwtOptionsAccessor();

    public AuthServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AuthMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepository
            .Setup(x => x.GetByUsernameAsync(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ThrowsUnauthorizedAccessException()
    {
        _userRepository
            .Setup(x => x.GetByUsernameAsync(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.CreateUser(isActive: false));

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, "WrongPassword!");

        _userRepository
            .Setup(x => x.GetByUsernameAsync(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.LoginAsync(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsAuthResponseAndStoresRefreshToken()
    {
        var user = TestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        _userRepository
            .Setup(x => x.GetByUsernameAsync(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenService
            .Setup(x => x.GenerateTokens(user))
            .Returns(("access-token", expiresAtUtc, "new-refresh-token"));

        RefreshToken? savedToken = null;
        _refreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.LoginAsync(new LoginRequest { Username = TestData.Username, Password = TestData.Password });

        Assert.Equal(user.UserId, result.User.UserId);
        Assert.Equal(user.Username, result.User.Username);
        Assert.Equal(user.FullName, result.User.FullName);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.Role.Name, result.User.RoleName);
        Assert.Equal("access-token", result.Tokens.AccessToken);
        Assert.Equal("new-refresh-token", result.Tokens.RefreshToken);
        Assert.Equal(expiresAtUtc, result.Tokens.AccessTokenExpiresAtUtc);

        _refreshTokenRepository.Verify(x => x.RemoveByUserIdAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedToken);
        Assert.Equal(user.UserId, savedToken!.UserId);
        Assert.Equal("new-refresh-token", savedToken.Token);
        Assert.True(savedToken.ExpiryDateUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenNotFound_ThrowsUnauthorizedAccessException()
    {
        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUserAsync("missing-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = "missing-token" }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsExpired_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser();
        var expiredToken = TestData.CreateRefreshToken(user, expiryDateUtc: DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUserAsync(expiredToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = expiredToken.Token }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenUserIsInactive_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser(isActive: false);
        var storedToken = TestData.CreateRefreshToken(user);

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUserAsync(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = storedToken.Token }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenIsValid_ReturnsNewTokensAndReplacesStoredToken()
    {
        var user = TestData.CreateUser();
        var storedToken = TestData.CreateRefreshToken(user);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUserAsync(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _jwtTokenService
            .Setup(x => x.GenerateTokens(user))
            .Returns(("rotated-access-token", expiresAtUtc, "rotated-refresh-token"));

        RefreshToken? savedToken = null;
        _refreshTokenRepository
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = storedToken.Token });

        Assert.Equal(user.UserId, result.User.UserId);
        Assert.Equal("rotated-access-token", result.Tokens.AccessToken);
        Assert.Equal("rotated-refresh-token", result.Tokens.RefreshToken);

        _refreshTokenRepository.Verify(x => x.RemoveByUserIdAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedToken);
        Assert.Equal("rotated-refresh-token", savedToken!.Token);
    }

    private AuthService CreateSut() =>
        new(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher,
            _jwtTokenService.Object,
            _mapper,
            _jwtOptions);
}
