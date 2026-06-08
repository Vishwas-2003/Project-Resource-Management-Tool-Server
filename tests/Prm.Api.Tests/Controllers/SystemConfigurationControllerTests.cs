using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.SystemConfigurations;

namespace Prm.Api.Tests.Controllers;

public class SystemConfigurationControllerTests
{
    private readonly Mock<ISystemConfigurationService> _systemConfigurationService = new();

    [Fact]
    public async Task Get_WhenConfigurationsExist_ReturnsOk()
    {
        var configurations = new List<SystemConfigurationResponse>
        {
            new() { Id = 1, ConfigurationType = "MaxWeeklyHours", Value = "40" },
        };

        _systemConfigurationService
            .Setup(x => x.GetAllConfigurations(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configurations);

        var sut = new SystemConfigurationController(_systemConfigurationService.Object);
        var result = await sut.Get(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = Assert.IsAssignableFrom<IReadOnlyList<SystemConfigurationResponse>>(okResult.Value);
        Assert.Single(value);
    }

    [Fact]
    public async Task Update_WhenConfigurationNotFound_Returns404()
    {
        _systemConfigurationService
            .Setup(x => x.Update(99, "40", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.SystemConfiguration.NotFound));

        var sut = new SystemConfigurationController(_systemConfigurationService.Object);
        var result = await sut.Update(99, "40", CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }
}
