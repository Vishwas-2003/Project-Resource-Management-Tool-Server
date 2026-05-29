using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Prm.Common.Constants;
using Prm.Common.Models.Auth;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using UserManagement.Configuration;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services;

public class AuthService(
    IUserRepository _userRepository,
    IRefreshTokenRepository _refreshTokenRepository,
    IPasswordHasher<User> _passwordHasher,
    IJwtTokenService _jwtTokenService,
    IMapper _mapper,
    IOptions<JwtOptions> _jwtOptionsAccessor) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.InvalidCredentials);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.InvalidCredentials);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenWithUserAsync(request.RefreshToken, cancellationToken);
        if (storedToken is null || storedToken.ExpiryDateUtc <= DateTime.UtcNow || !storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.RefreshTokenInvalidOrExpired);
        }

        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAtUtc, refreshTokenValue) = _jwtTokenService.GenerateTokens(user);

        await _refreshTokenRepository.RemoveByUserIdAsync(user.UserId, cancellationToken);

        await _refreshTokenRepository.AddAsync(
            new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshTokenValue,
                ExpiryDateUtc = DateTime.UtcNow.AddDays(_jwtOptionsAccessor.Value.RefreshTokenDays),
            },
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

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
