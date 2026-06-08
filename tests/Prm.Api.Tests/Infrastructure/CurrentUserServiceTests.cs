using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Prm.Api.Infrastructure;

namespace Prm.Api.Tests.Infrastructure;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetUserId_WhenNameIdentifierClaimPresent_ReturnsParsedUserId()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42")],
                authenticationType: "Test")),
        };

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var sut = new CurrentUserService(accessor);

        Assert.Equal(42, sut.GetUserId());
    }

    [Fact]
    public void GetUserId_WhenClaimMissing_ReturnsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new CurrentUserService(accessor);

        Assert.Null(sut.GetUserId());
    }

    [Fact]
    public void GetUserId_WhenHttpContextMissing_ReturnsNull()
    {
        var sut = new CurrentUserService(new HttpContextAccessor());

        Assert.Null(sut.GetUserId());
    }
}
