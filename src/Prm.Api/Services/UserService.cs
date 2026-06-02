using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Users;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class UserService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher<User> passwordHasher,
    IMapper mapper) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IMapper _mapper = mapper;

    public async Task<int> Add(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRole(request.RoleId, cancellationToken);
        ValidatePasswordStrength(request.TemporaryPassword);

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await _userRepository.ExistsByUsername(username, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Users.UsernameExists);
        }

        if (await _userRepository.ExistsByEmail(email, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Users.EmailExists);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Username = username,
            Email = email,
            RoleId = request.RoleId,
            PasswordHash = string.Empty,
            IsActive = true,
            ForcePasswordChange = true,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.TemporaryPassword);

        await _userRepository.Add(user, cancellationToken);
        await _userRepository.SaveChanges(cancellationToken);

        return user.Id;
    }

    public async Task<UserListResult> GetUsers(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetUsers(cancellationToken);
        var summaries = _mapper.Map<IReadOnlyList<UserSummary>>(users);

        return new UserListResult
        {
            Users = summaries,
            Total = summaries.Count,
            Active = summaries.Count(x => x.Status == UserConstants.StatusActive),
            Inactive = summaries.Count(x => x.Status == UserConstants.StatusInactive),
        };
    }

    public async Task<bool> Reactivate(int userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrow(userId, cancellationToken);

        if (user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Users.AlreadyActive);
        }

        user.IsActive = true;
        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> ResetPassword(
        ResetUserPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserOrThrow(request, cancellationToken);
        ValidatePasswordStrength(request.TemporaryPassword);

        user.PasswordHash = _passwordHasher.HashPassword(user, request.TemporaryPassword);
        user.ForcePasswordChange = true;

        await _refreshTokenRepository.RemoveByUserId(user.Id, cancellationToken);

        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<bool> Deactivate(
        UserLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await ResolveUserOrThrow(request, cancellationToken);

        if (await _userRepository.IsLastActiveAdmin(user, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Users.CannotDeactivateLastAdmin);
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(AppConstants.Users.AlreadyInactive);
        }

        user.IsActive = false;

        await _refreshTokenRepository.RemoveByUserId(user.Id, cancellationToken);

        _userRepository.Update(user);
        await _userRepository.SaveChanges(cancellationToken);

        return true;
    }

    private async Task ValidateRole(int roleId, CancellationToken cancellationToken)
    {
        if (roleId is not (int)RoleNameEnum.Admin
            and not (int)RoleNameEnum.Manager
            and not (int)RoleNameEnum.Employee)
        {
            throw new ArgumentException(AppConstants.Users.InvalidRole);
        }

        if (!await _roleRepository.Exists(roleId, cancellationToken))
        {
            throw new ArgumentException(AppConstants.Users.InvalidRole);
        }
    }

    private async Task<User> GetUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRole(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Users.NotFound);
        }

        return user;
    }

    private async Task<User> ResolveUserOrThrow(UserLookupRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId is int userId)
        {
            return await GetUserOrThrow(userId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var user = await _userRepository.GetByUsername(request.Username, cancellationToken);
            if (user is null)
            {
                throw new KeyNotFoundException(AppConstants.Users.NotFound);
            }

            return user;
        }

        throw new ArgumentException(AppConstants.Users.LookupRequired);
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (password.Length < 8
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
            || !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new ArgumentException(AppConstants.Auth.PasswordDoesNotMeetRequirements);
        }
    }
}
