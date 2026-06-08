using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Prm.Api.Infrastructure;
using Prm.Common.Models.Api;
using Prm.Data.Audit;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Tests.Helpers;

internal static class ControllerTestHelper
{
    internal static ManagerAccess CreateManagerAccess(
        int userId,
        User? activeManager = null)
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(x => x.GetUserId()).Returns(userId);

        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.GetActiveManagerById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeManager);

        return new ManagerAccess(currentUserService.Object, userRepository.Object);
    }

    internal static T AssertOkValue<T>(IActionResult result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<T>(okResult.Value);
    }

    internal static T AssertCreatedValue<T>(IActionResult result, int expectedStatusCode = StatusCodes.Status201Created)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        return Assert.IsType<T>(objectResult.Value);
    }

    internal static ApiErrorResponse AssertErrorResult(IActionResult result, int statusCode, string code)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(statusCode, objectResult.StatusCode);

        var error = Assert.IsType<ApiErrorResponse>(objectResult.Value);
        Assert.Equal(code, error.Code);
        return error;
    }
}
