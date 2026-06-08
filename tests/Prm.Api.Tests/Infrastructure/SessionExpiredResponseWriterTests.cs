using Microsoft.AspNetCore.Http;
using Prm.Api.Infrastructure;
using Prm.Common.Constants;

namespace Prm.Api.Tests.Infrastructure;

public class SessionExpiredResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_Sets401AndWritesSessionExpiredPayload()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await SessionExpiredResponseWriter.WriteAsync(httpContext.Response);

        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.StartsWith(AppConstants.Http.JsonContentType, httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        var json = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

        Assert.Contains(AppConstants.ErrorCodes.SessionExpired, json);
        Assert.Contains(AppConstants.Messages.SessionExpired, json);
    }
}
