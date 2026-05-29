using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prm.Common.Constants;
using Prm.Common.Models.Api;

namespace Prm.Api.Infrastructure;

public static class ControllerExceptionHandler
{
    public static IActionResult Handle(Exception exception, bool treatUnauthorizedAsSessionExpired = false)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var result = TryMap(current, treatUnauthorizedAsSessionExpired);
            if (result is not null)
            {
                return result;
            }
        }

        return InternalError();
    }

    private static IActionResult? TryMap(Exception exception, bool treatUnauthorizedAsSessionExpired) =>
        exception switch
        {
            UnauthorizedAccessException when treatUnauthorizedAsSessionExpired => SessionExpired(),
            UnauthorizedAccessException unauthorized => Unauthorized(unauthorized.Message),
            InvalidOperationException invalidOperation => BadRequest(invalidOperation.Message),
            ArgumentException argument => BadRequest(argument.Message),
            KeyNotFoundException notFound => NotFound(notFound.Message),
            DbUpdateConcurrencyException => Conflict(AppConstants.Messages.ConcurrencyConflict),
            DbUpdateException => BadRequest(AppConstants.Messages.DatabaseError),
            _ => null,
        };

    public static IActionResult BadRequest(string message) =>
        StatusCode(StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.BadRequest, message);

    public static IActionResult Unauthorized(string message) =>
        StatusCode(StatusCodes.Status401Unauthorized, AppConstants.ErrorCodes.Unauthorized, message);

    public static IActionResult NotFound(string message) =>
        StatusCode(StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound, message);

    public static IActionResult Conflict(string message) =>
        StatusCode(StatusCodes.Status409Conflict, AppConstants.ErrorCodes.Conflict, message);

    public static IActionResult SessionExpired() =>
        StatusCode(
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.SessionExpired,
            AppConstants.Messages.SessionExpired);

    public static IActionResult InternalError() =>
        StatusCode(
            StatusCodes.Status500InternalServerError,
            AppConstants.ErrorCodes.InternalError,
            AppConstants.Messages.InternalError);

    private static ObjectResult StatusCode(int statusCode, string code, string message) =>
        new(new ApiErrorResponse { Code = code, Message = message }) { StatusCode = statusCode };
}
