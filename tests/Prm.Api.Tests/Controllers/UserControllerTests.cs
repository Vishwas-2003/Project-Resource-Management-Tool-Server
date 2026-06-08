using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.Users;

namespace Prm.Api.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userService = new();

    [Fact]
    public async Task Add_WhenValid_ReturnsCreated()
    {
        _userService
            .Setup(x => x.Add(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var sut = new UserController(_userService.Object);
        var result = await sut.Add(
            new CreateUserRequest
            {
                FullName = "Jane Doe",
                Username = "jdoe",
                Email = "jdoe@prm.local",
                RoleId = 3,
            },
            CancellationToken.None);

        Assert.Equal(5, ControllerTestHelper.AssertCreatedValue<CreatedIdResponse>(result).Id);
    }

    [Fact]
    public async Task GetUsers_WhenUsersExist_ReturnsOk()
    {
        var response = new UserListResult
        {
            Users = [new UserSummary { Id = 1, Username = "jdoe", Role = "Employee", Status = "Active" }],
        };

        _userService
            .Setup(x => x.GetUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new UserController(_userService.Object);
        var result = await sut.GetUsers(CancellationToken.None);

        Assert.Single(ControllerTestHelper.AssertOkValue<UserListResult>(result).Users);
    }

    [Fact]
    public async Task Reactivate_WhenValid_ReturnsOk()
    {
        _userService
            .Setup(x => x.Reactivate(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new UserController(_userService.Object);
        var result = await sut.Reactivate(1, CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task ResetPassword_WhenValid_ReturnsOk()
    {
        _userService
            .Setup(x => x.ResetPassword(It.IsAny<ResetUserPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new UserController(_userService.Object);
        var result = await sut.ResetPassword(
            new ResetUserPasswordRequest { UserId = 1, TemporaryPassword = ApiTestData.ValidPassword },
            CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task Deactivate_WhenUserNotFound_Returns404()
    {
        _userService
            .Setup(x => x.Deactivate(It.IsAny<UserLookupRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Users.NotFound));

        var sut = new UserController(_userService.Object);
        var result = await sut.Deactivate(
            new UserLookupRequest { UserId = 99 },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Add_WhenUsernameExists_Returns400()
    {
        _userService
            .Setup(x => x.Add(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(AppConstants.Users.UsernameExists));

        var sut = new UserController(_userService.Object);
        var result = await sut.Add(
            new CreateUserRequest
            {
                FullName = "Jane Doe",
                Username = "jdoe",
                Email = "jdoe@prm.local",
                RoleId = 3,
            },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest);
    }
}
