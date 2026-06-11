using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.Resources;
using Prm.Common.Models.Manager;

namespace Prm.Api.Tests.Controllers;

public class ResourceControllerTests
{
    private readonly Mock<IResourceService> _resourceService = new();

    [Fact]
    public async Task GetResources_WhenResourcesExist_ReturnsOk()
    {
        var response = new ResourceListResult
        {
            Total = 1,
            Resources = [new ResourceSummary { Id = 1, FullName = "Jane Doe" }],
        };

        _resourceService
            .Setup(x => x.GetResources(It.IsAny<ResourceFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.GetResources(new ResourceFilter(), CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<ResourceListResult>(result);
        Assert.Equal(1, value.Total);
    }

    [Fact]
    public async Task AssignManager_WhenValid_ReturnsOk()
    {
        _resourceService
            .Setup(x => x.AssignManager(It.IsAny<AssignManagerRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.AssignManager(
            new AssignManagerRequest
            {
                ResourceUserId = 1,
                ManagerUserId = 10,
                Department = "Engineering",
                Designation = "Developer",
            },
            CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<UpdatedResponse>(result);
        Assert.True(value.Updated);
    }

    [Fact]
    public async Task Update_WhenValid_ReturnsOk()
    {
        _resourceService
            .Setup(x => x.Update(1, It.IsAny<UpdateResourceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.Update(
            1,
            new UpdateResourceRequest { Department = "Engineering", Designation = "Developer" },
            CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task Deactivate_WhenValid_ReturnsOk()
    {
        _resourceService
            .Setup(x => x.Deactivate(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.Deactivate(1, CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task GetDetail_WhenResourceNotFound_Returns404()
    {
        _resourceService
            .Setup(x => x.GetDetail(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Resources.NotFound));

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.GetDetail(99, CancellationToken.None);

        var error = ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
        Assert.Equal(AppConstants.Resources.NotFound, error.Message);
    }

    [Fact]
    public async Task GetUtilization_WhenValid_ReturnsOk()
    {
        var response = new ResourceUtilizationResponse { ResourceUserId = 1, UtilizationPercent = 80 };

        _resourceService
            .Setup(x => x.GetUtilization(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new ResourceController(_resourceService.Object);
        var result = await sut.GetUtilization(1, CancellationToken.None);

        Assert.Equal(80, ControllerTestHelper.AssertOkValue<ResourceUtilizationResponse>(result).UtilizationPercent);
    }
}
