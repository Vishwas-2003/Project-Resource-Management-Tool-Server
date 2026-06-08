using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models.Auth;
using UserManagement.Services.Interfaces;

namespace Prm.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsOkWithAuthResponse()
    {
        var authResponse = new AuthResponse
        {
            User = new AuthenticatedUser { UserId = 1, Username = "jdoe" },
            Tokens = new AuthTokens { AccessToken = "access", RefreshToken = "refresh" },
        };

        _authService
            .Setup(x => x.Login(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var sut = new AuthController(_authService.Object);
        var result = await sut.Login(
            new LoginRequest { Username = "jdoe", Password = ApiTestData.ValidPassword },
            CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<AuthResponse>(result);
        Assert.Equal("jdoe", value.User.Username);
        Assert.Equal("access", value.Tokens.AccessToken);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_Returns401()
    {
        _authService
            .Setup(x => x.Login(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException(AppConstants.Auth.InvalidCredentials));

        var sut = new AuthController(_authService.Object);
        var result = await sut.Login(
            new LoginRequest { Username = "jdoe", Password = "wrong" },
            CancellationToken.None);

        var error = ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.Unauthorized);
        Assert.Equal(AppConstants.Auth.InvalidCredentials, error.Message);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ReturnsOkWithAuthResponse()
    {
        var authResponse = new AuthResponse
        {
            User = new AuthenticatedUser { UserId = 1, Username = "jdoe" },
            Tokens = new AuthTokens { AccessToken = "new-access", RefreshToken = "new-refresh" },
        };

        _authService
            .Setup(x => x.Refresh(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var sut = new AuthController(_authService.Object);
        var result = await sut.Refresh(
            new RefreshTokenRequest { RefreshToken = "token" },
            CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<AuthResponse>(result);
        Assert.Equal("new-access", value.Tokens.AccessToken);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsInvalid_ReturnsSessionExpired()
    {
        _authService
            .Setup(x => x.Refresh(It.IsAny<RefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException(AppConstants.Auth.RefreshTokenInvalidOrExpired));

        var sut = new AuthController(_authService.Object);
        var result = await sut.Refresh(
            new RefreshTokenRequest { RefreshToken = "expired" },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task ChangePassword_WhenRequired_ReturnsOk()
    {
        var authResponse = new AuthResponse
        {
            User = new AuthenticatedUser { UserId = 1, Username = "jdoe" },
            Tokens = new AuthTokens { AccessToken = "access", RefreshToken = "refresh" },
        };

        _authService
            .Setup(x => x.ChangePassword(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var sut = new AuthController(_authService.Object);
        var result = await sut.ChangePassword(
            new ChangePasswordRequest { NewPassword = ApiTestData.ValidPassword, ConfirmPassword = ApiTestData.ValidPassword },
            CancellationToken.None);

        ControllerTestHelper.AssertOkValue<AuthResponse>(result);
    }

    [Fact]
    public async Task ChangePassword_WhenNotRequired_Returns400()
    {
        _authService
            .Setup(x => x.ChangePassword(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(AppConstants.Auth.PasswordChangeNotRequired));

        var sut = new AuthController(_authService.Object);
        var result = await sut.ChangePassword(
            new ChangePasswordRequest { NewPassword = ApiTestData.ValidPassword, ConfirmPassword = ApiTestData.ValidPassword },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest);
    }
}
