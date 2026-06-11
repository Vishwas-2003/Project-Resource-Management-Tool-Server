using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Auth;
using Prm.Data.Audit;
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
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IMapper _mapper;
    private readonly IOptions<JwtOptions> _jwtOptions = TestData.CreateJwtOptionsAccessor();

    public AuthServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AuthMappingProfile>());
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepository
            .Setup(x => x.GetByUsername(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Login(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task Login_WhenUserIsInactive_ThrowsUnauthorizedAccessException()
    {
        _userRepository
            .Setup(x => x.GetByUsername(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.CreateUser(isActive: false));

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Login(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InactiveUser, exception.Message);
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, "WrongPassword!");

        _userRepository
            .Setup(x => x.GetByUsername(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Login(new LoginRequest { Username = TestData.Username, Password = TestData.Password }));

        Assert.Equal(AppConstants.Auth.InvalidCredentials, exception.Message);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsAuthResponseAndStoresRefreshToken()
    {
        var user = TestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        _userRepository
            .Setup(x => x.GetByUsername(TestData.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenService
            .Setup(x => x.GenerateTokens(user))
            .Returns(("access-token", expiresAtUtc, "new-refresh-token"));

        RefreshToken? savedToken = null;
        _refreshTokenRepository
            .Setup(x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Login(new LoginRequest { Username = TestData.Username, Password = TestData.Password });

        Assert.Equal(user.Id, result.User.UserId);
        Assert.Equal(user.Username, result.User.Username);
        Assert.Equal(user.FullName, result.User.FullName);
        Assert.Equal(user.Email, result.User.Email);
        Assert.Equal(user.Role.Name, result.User.RoleName);
        Assert.Equal("access-token", result.Tokens.AccessToken);
        Assert.Equal("new-refresh-token", result.Tokens.RefreshToken);
        Assert.Equal(expiresAtUtc, result.Tokens.AccessTokenExpiresAtUtc);

        _refreshTokenRepository.Verify(x => x.RemoveByUserId(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedToken);
        Assert.Equal(user.Id, savedToken!.UserId);
        Assert.Equal("new-refresh-token", savedToken.Token);
        Assert.True(savedToken.ExpiryDateUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_WhenTokenNotFound_ThrowsUnauthorizedAccessException()
    {
        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUser("missing-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Refresh(new RefreshTokenRequest { RefreshToken = "missing-token" }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsExpired_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser();
        var expiredToken = TestData.CreateRefreshToken(user, expiryDateUtc: DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUser(expiredToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Refresh(new RefreshTokenRequest { RefreshToken = expiredToken.Token }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task Refresh_WhenUserIsInactive_ThrowsUnauthorizedAccessException()
    {
        var user = TestData.CreateUser(isActive: false);
        var storedToken = TestData.CreateRefreshToken(user);

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUser(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Refresh(new RefreshTokenRequest { RefreshToken = storedToken.Token }));

        Assert.Equal(AppConstants.Auth.RefreshTokenInvalidOrExpired, exception.Message);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ReturnsNewTokensAndReplacesStoredToken()
    {
        var user = TestData.CreateUser();
        var storedToken = TestData.CreateRefreshToken(user);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        _refreshTokenRepository
            .Setup(x => x.GetByTokenWithUser(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _jwtTokenService
            .Setup(x => x.GenerateTokens(user))
            .Returns(("rotated-access-token", expiresAtUtc, "rotated-refresh-token"));

        RefreshToken? savedToken = null;
        _refreshTokenRepository
            .Setup(x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Refresh(new RefreshTokenRequest { RefreshToken = storedToken.Token });

        Assert.Equal(user.Id, result.User.UserId);
        Assert.Equal("rotated-access-token", result.Tokens.AccessToken);
        Assert.Equal("rotated-refresh-token", result.Tokens.RefreshToken);

        _refreshTokenRepository.Verify(x => x.RemoveByUserId(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(savedToken);
        Assert.Equal("rotated-refresh-token", savedToken!.Token);
    }

    [Fact]
    public async Task ChangePassword_WhenUserIsNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        _currentUserService.Setup(x => x.GetUserId()).Returns((int?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ChangePassword(new ChangePasswordRequest { NewPassword = "NewPass1", ConfirmPassword = "NewPass1" }));

        Assert.Equal(AppConstants.Auth.UserNotAuthenticated, exception.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenPasswordChangeNotRequired_ThrowsInvalidOperationException()
    {
        var user = TestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);

        _currentUserService.Setup(x => x.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ChangePassword(new ChangePasswordRequest { NewPassword = "ValidPass1!", ConfirmPassword = "ValidPass1!" }));

        Assert.Equal(AppConstants.Auth.PasswordChangeNotRequired, exception.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenPasswordsDoNotMatch_ThrowsArgumentException()
    {
        var user = TestData.CreateUser();
        user.PasswordExpiryTime = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);

        _currentUserService.Setup(x => x.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ChangePassword(new ChangePasswordRequest { NewPassword = "NewPass1", ConfirmPassword = "OtherPass1" }));

        Assert.Equal(AppConstants.Auth.PasswordsDoNotMatch, exception.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenPasswordDoesNotMeetRequirements_ThrowsArgumentException()
    {
        var user = TestData.CreateUser();
        user.PasswordExpiryTime = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);

        _currentUserService.Setup(x => x.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ChangePassword(new ChangePasswordRequest { NewPassword = "NewPass1", ConfirmPassword = "NewPass1" }));

        Assert.Equal(AppConstants.Auth.PasswordDoesNotMeetRequirements, exception.Message);
    }

    [Fact]
    public async Task ChangePassword_WhenSuccessful_UpdatesPasswordClearsFlagAndReturnsTokens()
    {
        const string newPassword = "NewPass1!";
        var user = TestData.CreateUser();
        user.PasswordExpiryTime = DateTime.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(user, TestData.Password);
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(30);

        _currentUserService.Setup(x => x.GetUserId()).Returns(user.Id);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenService
            .Setup(x => x.GenerateTokens(user))
            .Returns(("access-token", expiresAtUtc, "new-refresh-token"));

        _refreshTokenRepository
            .Setup(x => x.Add(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.ChangePassword(
            new ChangePasswordRequest { NewPassword = newPassword, ConfirmPassword = newPassword });

        Assert.Null(user.PasswordExpiryTime);
        Assert.Equal(PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword));
        Assert.Null(result.User.PasswordExpiryTime);
        Assert.Equal("access-token", result.Tokens.AccessToken);
        Assert.Equal("new-refresh-token", result.Tokens.RefreshToken);

        _userRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenRepository.Verify(x => x.RemoveByUserId(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private AuthService CreateSut() =>
        new(
            _userRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher,
            _jwtTokenService.Object,
            _currentUserService.Object,
            _mapper,
            _jwtOptions);
}
