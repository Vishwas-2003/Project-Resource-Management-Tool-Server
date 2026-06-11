using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Users;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly IPasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task Add_WhenInvalidRoleId_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(new CreateUserRequest
            {
                FullName = "Test",
                Email = "t@prm.local",
                Username = "testuser",
                TemporaryPassword = ApiTestData.ValidPassword,
                RoleId = 99,
            }));

        Assert.Equal(AppConstants.Users.InvalidRole, exception.Message);
    }

    [Fact]
    public async Task Add_WhenRoleDoesNotExist_ThrowsArgumentException()
    {
        _roleRepository
            .Setup(x => x.Exists((int)RoleNameEnum.Employee, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(new CreateUserRequest
            {
                FullName = "Test",
                Email = "t@prm.local",
                Username = "testuser",
                TemporaryPassword = ApiTestData.ValidPassword,
                RoleId = (int)RoleNameEnum.Employee,
            }));

        Assert.Equal(AppConstants.Users.InvalidRole, exception.Message);
    }

    [Fact]
    public async Task Add_WhenPasswordTooWeak_ThrowsArgumentException()
    {
        SetupValidRole();

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(new CreateUserRequest
            {
                FullName = "Test",
                Email = "t@prm.local",
                Username = "testuser",
                TemporaryPassword = "weak",
                RoleId = (int)RoleNameEnum.Employee,
            }));

        Assert.Equal(AppConstants.Auth.PasswordDoesNotMeetRequirements, exception.Message);
    }

    [Fact]
    public async Task Add_WhenUsernameExists_ThrowsInvalidOperationException()
    {
        SetupValidRole();
        _userRepository
            .Setup(x => x.ExistsByUsername("existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(new CreateUserRequest
            {
                FullName = "Test",
                Email = "t@prm.local",
                Username = "existing",
                TemporaryPassword = ApiTestData.ValidPassword,
                RoleId = (int)RoleNameEnum.Employee,
            }));

        Assert.Equal(AppConstants.Users.UsernameExists, exception.Message);
    }

    [Fact]
    public async Task Add_WhenEmailExists_ThrowsInvalidOperationException()
    {
        SetupValidRole();
        _userRepository
            .Setup(x => x.ExistsByUsername(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository
            .Setup(x => x.ExistsByEmail("taken@prm.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(new CreateUserRequest
            {
                FullName = "Test",
                Email = "taken@prm.local",
                Username = "newuser",
                TemporaryPassword = ApiTestData.ValidPassword,
                RoleId = (int)RoleNameEnum.Employee,
            }));

        Assert.Equal(AppConstants.Users.EmailExists, exception.Message);
    }

    [Fact]
    public async Task Add_WhenSuccessful_SetsPasswordExpiryTimeAndReturnsId()
    {
        SetupValidRole();
        _userRepository
            .Setup(x => x.ExistsByUsername(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        User? saved = null;
        _userRepository
            .Setup(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) =>
            {
                user.Id = 7;
                saved = user;
            })
            .Returns(Task.CompletedTask);
        _userRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepository
            .Setup(x => x.SetCurrentResourceStatus(
                It.IsAny<int>(),
                (int)ResourceStatusTypeEnum.Bench,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var id = await sut.Add(new CreateUserRequest
        {
            FullName = "New User",
            Email = "new@prm.local",
            Username = "newuser",
            TemporaryPassword = ApiTestData.ValidPassword,
            RoleId = (int)RoleNameEnum.Employee,
        });

        Assert.Equal(7, id);
        Assert.NotNull(saved);
        Assert.NotNull(saved!.PasswordExpiryTime);
        Assert.True(saved.IsActive);
        Assert.Equal(PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(saved, saved.PasswordHash, ApiTestData.ValidPassword));
        _userRepository.Verify(
            x => x.SetCurrentResourceStatus(7, (int)ResourceStatusTypeEnum.Bench, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Add_WhenManagerRole_DoesNotSetBenchStatus()
    {
        SetupValidRole();
        _userRepository
            .Setup(x => x.ExistsByUsername(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository
            .Setup(x => x.ExistsByEmail(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepository
            .Setup(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => user.Id = 8)
            .Returns(Task.CompletedTask);
        _userRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.Add(new CreateUserRequest
        {
            FullName = "Manager User",
            Email = "manager@prm.local",
            Username = "manageruser",
            TemporaryPassword = ApiTestData.ValidPassword,
            RoleId = (int)RoleNameEnum.Manager,
        });

        _userRepository.Verify(
            x => x.SetCurrentResourceStatus(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUsers_ReturnsActiveAndInactiveCounts()
    {
        var users = new List<User>
        {
            ApiTestData.CreateUser(1, isActive: true),
            ApiTestData.CreateUser(2, isActive: false, username: "inactive"),
        };

        _userRepository
            .Setup(x => x.GetUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var sut = CreateSut();
        var result = await sut.GetUsers();

        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.Active);
        Assert.Equal(1, result.Inactive);
    }

    [Fact]
    public async Task Reactivate_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetByIdWithRole(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Reactivate(1));

        Assert.Equal(AppConstants.Users.NotFound, exception.Message);
    }

    [Fact]
    public async Task Reactivate_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateUser(isActive: true);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Reactivate(user.Id));

        Assert.Equal(AppConstants.Users.AlreadyActive, exception.Message);
    }

    [Fact]
    public async Task Reactivate_WhenSuccessful_ReturnsTrue()
    {
        var user = ApiTestData.CreateUser(isActive: false);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.Reactivate(user.Id);

        Assert.True(result);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyInactive_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateUser(isActive: false);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Deactivate(new UserLookupRequest { UserId = user.Id }));

        Assert.Equal(AppConstants.Users.AlreadyInactive, exception.Message);
    }

    [Fact]
    public async Task Deactivate_WhenLastActiveAdmin_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateUser(roleId: (int)RoleNameEnum.Admin);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(x => x.IsLastActiveAdmin(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Deactivate(new UserLookupRequest { UserId = user.Id }));

        Assert.Equal(AppConstants.Users.CannotDeactivateLastAdmin, exception.Message);
    }

    [Fact]
    public async Task Deactivate_WhenResolvedByUsername_DeactivatesUser()
    {
        var user = ApiTestData.CreateUser(isActive: true);
        _userRepository
            .Setup(x => x.GetByUsername(user.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(x => x.IsLastActiveAdmin(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.Deactivate(new UserLookupRequest { Username = user.Username });

        Assert.True(result);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenLookupMissing_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Deactivate(new UserLookupRequest()));

        Assert.Equal(AppConstants.Users.LookupRequired, exception.Message);
    }

    [Fact]
    public async Task Deactivate_WhenSuccessful_ClearsRefreshTokens()
    {
        var user = ApiTestData.CreateUser(isActive: true);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.Deactivate(new UserLookupRequest { UserId = user.Id });

        Assert.True(result);
        Assert.False(user.IsActive);
        _refreshTokenRepository.Verify(x => x.RemoveByUserId(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WhenLookupMissing_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ResetPassword(new ResetUserPasswordRequest { TemporaryPassword = ApiTestData.ValidPassword }));

        Assert.Equal(AppConstants.Users.LookupRequired, exception.Message);
    }

    [Fact]
    public async Task ResetPassword_WhenSuccessful_UpdatesHashAndClearsTokens()
    {
        var user = ApiTestData.CreateUser();
        user.PasswordHash = _passwordHasher.HashPassword(user, "OldPass1!");

        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.ResetPassword(new ResetUserPasswordRequest
        {
            UserId = user.Id,
            TemporaryPassword = ApiTestData.ValidPassword,
        });

        Assert.True(result);
        Assert.NotNull(user.PasswordExpiryTime);
        Assert.Equal(PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, ApiTestData.ValidPassword));
        _refreshTokenRepository.Verify(x => x.RemoveByUserId(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupValidRole()
    {
        _roleRepository
            .Setup(x => x.Exists(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private UserService CreateSut() =>
        new(
            _userRepository.Object,
            _roleRepository.Object,
            _refreshTokenRepository.Object,
            _passwordHasher,
            _mapper);
}
