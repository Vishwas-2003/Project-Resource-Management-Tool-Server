using Prm.Common.Constants;
using Prm.Common.Models.Api;

namespace Prm.Api.Infrastructure;

public static class PasswordChangeRequiredResponseWriter
{
    public static Task WriteAsync(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status403Forbidden;
        response.ContentType = AppConstants.Http.JsonContentType;

        return response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = AppConstants.ErrorCodes.PasswordChangeRequired,
            Message = AppConstants.Auth.PasswordChangeRequired,
        });
    }
}
