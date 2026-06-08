using Moq;
using Prm.Api.Infrastructure;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Audit;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Tests.Infrastructure;

public class ManagerAccessTests
{
    [Fact]
    public void GetCurrentUserId_WhenNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.GetUserId()).Returns((int?)null);

        var sut = new ManagerAccess(currentUserService.Object, new Mock<IUserRepository>().Object);

        var exception = Assert.Throws<UnauthorizedAccessException>(() => sut.GetCurrentUserId());
        Assert.Equal(AppConstants.Auth.UserNotAuthenticated, exception.Message);
    }

    [Fact]
    public async Task GetCurrentManagerUserId_WhenManagerNotFound_ThrowsKeyNotFoundException()
    {
        const int userId = 10;
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.GetUserId()).Returns(userId);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetActiveManagerById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prm.Data.Entities.User?)null);

        var sut = new ManagerAccess(currentUserService.Object, userRepository.Object);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetCurrentManagerUserId());

        Assert.Equal(AppConstants.Manager.ProfileNotFound, exception.Message);
    }

    [Fact]
    public async Task GetCurrentManagerUserId_WhenManagerExists_ReturnsUserId()
    {
        const int userId = 10;
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.GetUserId()).Returns(userId);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetActiveManagerById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateUser(userId, (int)RoleNameEnum.Manager));

        var sut = new ManagerAccess(currentUserService.Object, userRepository.Object);
        var result = await sut.GetCurrentManagerUserId();

        Assert.Equal(userId, result);
    }
}
