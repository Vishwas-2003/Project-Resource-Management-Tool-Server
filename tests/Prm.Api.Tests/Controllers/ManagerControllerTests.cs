using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;

namespace Prm.Api.Tests.Controllers;

public class ManagerControllerTests
{
    private readonly Mock<IManagerService> _managerService = new();

    [Fact]
    public async Task GetResourceDashboard_WhenManagerIsAuthenticated_ReturnsOk()
    {
        const int managerUserId = 10;
        var response = new ResourceDashboardResponse
        {
            PeriodLabel = "Jun 2026",
            Summary = new ResourceDashboardSummary { BenchCount = 2, PartialCount = 3 },
        };

        _managerService
            .Setup(x => x.GetResourceDashboard(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new ManagerController(
            _managerService.Object,
            ControllerTestHelper.CreateManagerAccess(managerUserId, ApiTestData.CreateUser(managerUserId, (int)RoleNameEnum.Manager)));

        var result = await sut.GetResourceDashboard(CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<ResourceDashboardResponse>(result);
        Assert.Equal(2, value.Summary.BenchCount);
    }

    [Fact]
    public async Task GetResourceDashboard_WhenUserNotAuthenticated_Returns401()
    {
        var currentUser = new Mock<Prm.Data.Audit.ICurrentUserService>();
        currentUser.Setup(x => x.GetUserId()).Returns((int?)null);
        var userRepository = new Mock<Prm.Data.Repositories.Interfaces.IUserRepository>();
        var managerAccess = new Prm.Api.Infrastructure.ManagerAccess(currentUser.Object, userRepository.Object);

        var sut = new ManagerController(_managerService.Object, managerAccess);
        var result = await sut.GetResourceDashboard(CancellationToken.None);

        var error = ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.Unauthorized);
        Assert.Equal(AppConstants.Auth.UserNotAuthenticated, error.Message);
    }
}
