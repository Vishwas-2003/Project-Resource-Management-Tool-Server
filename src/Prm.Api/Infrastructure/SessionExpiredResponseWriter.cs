using Prm.Common.Constants;
using Prm.Common.Models.Api;

namespace Prm.Api.Infrastructure;

public static class SessionExpiredResponseWriter
{
    public static Task WriteAsync(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = AppConstants.Http.JsonContentType;

        return response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = AppConstants.ErrorCodes.SessionExpired,
            Message = AppConstants.Messages.SessionExpired,
        });
    }
}
