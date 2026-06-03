using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Auth;
using Prm.Data.Audit;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using UserManagement.Configuration;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services;

public class AuthService(
    IUserRepository _userRepository,
    IEmployeeRepository _employeeRepository,
    IRefreshTokenRepository _refreshTokenRepository,
    IPasswordHasher<User> _passwordHasher,
    IJwtTokenService _jwtTokenService,
    ICurrentUserService _currentUserService,
    IMapper _mapper,
    IOptions<JwtOptions> _jwtOptionsAccessor) : IAuthService
{
    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsername(request.Username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.InvalidCredentials);
        }

        var employee = await _employeeRepository.GetEmployeeByUserId(user.Id, cancellationToken);

        if (user.RoleId != (int)RoleNameEnum.Admin && employee is null)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.EmployeeProfileNotFound);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.InvalidCredentials);
        }

        return await IssueTokens(user, cancellationToken);
    }

    public async Task<AuthResponse> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenWithUser(request.RefreshToken, cancellationToken);
        if (storedToken is null || storedToken.ExpiryDateUtc <= DateTime.UtcNow || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.RefreshTokenInvalidOrExpired);
        }

        return await IssueTokens(storedToken.User, cancellationToken);
    }

    public async Task<AuthResponse> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.GetUserId();
        if (userId is null)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.UserNotAuthenticated);
        }

        var user = await _userRepository.GetByIdWithRole(userId.Value, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.UserNotAuthenticated);
        }

        if (!user.ForcePasswordChange)
        {
            throw new InvalidOperationException(AppConstants.Auth.PasswordChangeNotRequired);
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException(AppConstants.Auth.PasswordsDoNotMatch);
        }

        ValidatePasswordStrength(request.NewPassword);

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword)
            != PasswordVerificationResult.Failed)
        {
            throw new InvalidOperationException(AppConstants.Auth.NewPasswordMustDiffer);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.ForcePasswordChange = false;
        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return await IssueTokens(user, cancellationToken);
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
            || !password.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new ArgumentException(AppConstants.Auth.PasswordDoesNotMeetRequirements);
        }
    }

    private async Task<AuthResponse> IssueTokens(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAtUtc, refreshTokenValue) = _jwtTokenService.GenerateTokens(user);

        await _refreshTokenRepository.RemoveByUserId(user.Id, cancellationToken);

        await _refreshTokenRepository.Add(
            new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiryDateUtc = DateTime.UtcNow.AddDays(_jwtOptionsAccessor.Value.RefreshTokenDays),
                CreatedByUserId = user.Id,
            },
            cancellationToken);

        await _refreshTokenRepository.SaveChanges(cancellationToken);

        return new AuthResponse
        {
            User = _mapper.Map<AuthenticatedUser>(user),
            Tokens = new AuthTokens
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                AccessTokenExpiresAtUtc = expiresAtUtc,
            },
        };
    }
}
