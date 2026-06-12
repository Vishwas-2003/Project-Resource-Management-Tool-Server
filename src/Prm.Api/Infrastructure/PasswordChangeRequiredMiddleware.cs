using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Prm.Common.Auth;
using Prm.Common.Constants;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Infrastructure;

public sealed class PasswordChangeRequiredMiddleware(RequestDelegate _next)
{
    public async Task InvokeAsync(HttpContext context, IUserRepository _userRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        if (IsChangePasswordRequest(context))
        {
            await _next(context);
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            await _next(context);
            return;
        }

        var user = await _userRepository.GetById(userId, context.RequestAborted);
        if (user is not null && PasswordChangeRules.IsRequired(user.PasswordExpiryTime))
        {
            await PasswordChangeRequiredResponseWriter.WriteAsync(context.Response);
            return;
        }

        await _next(context);
    }

    private static bool IsChangePasswordRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments(
            $"/api/auth/{ApiRoutes.Auth.ChangePassword}",
            StringComparison.OrdinalIgnoreCase);
}
