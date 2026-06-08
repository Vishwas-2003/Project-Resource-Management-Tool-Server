using System.Text;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Prm.Api.Configuration;
using Prm.Api.Infrastructure;

namespace Prm.Api.Tests.Infrastructure;

public class HangfireDashboardAuthorizationFilterTests
{
    private readonly HangfireOptions _options = new()
    {
        DashboardUsername = "admin",
        DashboardPassword = "secret",
        DashboardPath = "/hangfire",
    };

    [Fact]
    public void Authorize_WhenCredentialsAreValid_ReturnsTrue()
    {
        var httpContext = CreateHttpContext("admin", "secret");
        var sut = CreateSut();

        Assert.True(sut.Authorize(CreateDashboardContext(httpContext)));
    }

    [Fact]
    public void Authorize_WhenCredentialsAreInvalid_ReturnsFalseAndSets401()
    {
        var httpContext = CreateHttpContext("admin", "wrong");
        var sut = CreateSut();

        Assert.False(sut.Authorize(CreateDashboardContext(httpContext)));
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Contains("Basic", httpContext.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public void Authorize_WhenHeaderMissing_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        var sut = CreateSut();

        Assert.False(sut.Authorize(CreateDashboardContext(httpContext)));
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public void Authorize_WhenHeaderIsMalformed_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Basic not-valid-base64!!!";
        var sut = CreateSut();

        Assert.False(sut.Authorize(CreateDashboardContext(httpContext)));
    }

    [Fact]
    public void Authorize_WhenDecodedCredentialsMissingSeparator_ReturnsFalse()
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("adminonly"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Basic {credentials}";
        var sut = CreateSut();

        Assert.False(sut.Authorize(CreateDashboardContext(httpContext)));
    }

    private HangfireDashboardAuthorizationFilter CreateSut() =>
        new(Options.Create(_options));

    private static DefaultHttpContext CreateHttpContext(string username, string password)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Basic {credentials}";
        return httpContext;
    }

    private static DashboardContext CreateDashboardContext(HttpContext httpContext)
    {
        var storage = new Mock<JobStorage>();
        return new AspNetCoreDashboardContext(
            storage.Object,
            new DashboardOptions { IgnoreAntiforgeryToken = true },
            httpContext);
    }
}
