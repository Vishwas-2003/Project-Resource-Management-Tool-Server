using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prm.Api.Infrastructure;
using Prm.Common.Constants;
using Prm.Common.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Tests.Infrastructure;

public class ControllerExceptionHandlerTests
{
    [Fact]
    public void Handle_WhenKeyNotFoundException_Returns404()
    {
        var result = ControllerExceptionHandler.Handle(
            new KeyNotFoundException(AppConstants.Projects.NotFound));

        AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound,
            AppConstants.Projects.NotFound);
    }

    [Fact]
    public void Handle_WhenArgumentException_Returns400()
    {
        var result = ControllerExceptionHandler.Handle(
            new ArgumentException(AppConstants.Allocations.InvalidDateRange));

        AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest,
            AppConstants.Allocations.InvalidDateRange);
    }

    [Fact]
    public void Handle_WhenUnauthorizedAccessException_Returns401()
    {
        var result = ControllerExceptionHandler.Handle(
            new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned));

        AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.Unauthorized,
            AppConstants.Manager.ProjectNotOwned);
    }

    [Fact]
    public void Handle_WhenInvalidOperationException_Returns400()
    {
        var result = ControllerExceptionHandler.Handle(
            new InvalidOperationException(AppConstants.Allocations.AlreadyEnded));

        AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest,
            AppConstants.Allocations.AlreadyEnded);
    }

    [Fact]
    public void Handle_WhenValidationException_Returns400()
    {
        const string message = "Field is required.";
        var result = ControllerExceptionHandler.Handle(new ValidationException(message));

        AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest,
            message);
    }

    [Fact]
    public void Handle_WhenDbUpdateConcurrencyException_Returns409()
    {
        var result = ControllerExceptionHandler.Handle(new DbUpdateConcurrencyException());

        AssertErrorResult(
            result,
            StatusCodes.Status409Conflict,
            AppConstants.ErrorCodes.Conflict,
            AppConstants.Messages.ConcurrencyConflict);
    }

    [Fact]
    public void Handle_WhenUnknownException_Returns500()
    {
        var result = ControllerExceptionHandler.Handle(new Exception("unexpected"));

        AssertErrorResult(
            result,
            StatusCodes.Status500InternalServerError,
            AppConstants.ErrorCodes.InternalError,
            AppConstants.Messages.InternalError);
    }

    [Fact]
    public void Handle_WhenUnauthorizedAndTreatAsSessionExpired_ReturnsSessionExpired()
    {
        var result = ControllerExceptionHandler.Handle(
            new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned),
            treatUnauthorizedAsSessionExpired: true);

        AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.SessionExpired,
            AppConstants.Messages.SessionExpired);
    }

    [Fact]
    public void BadRequest_Returns400WithMessage()
    {
        var result = ControllerExceptionHandler.BadRequest(AppConstants.Allocations.InvalidUtilization);

        AssertErrorResult(
            result,
            StatusCodes.Status400BadRequest,
            AppConstants.ErrorCodes.BadRequest,
            AppConstants.Allocations.InvalidUtilization);
    }

    [Fact]
    public void Unauthorized_Returns401WithMessage()
    {
        var result = ControllerExceptionHandler.Unauthorized(AppConstants.Manager.ProjectNotOwned);

        AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.Unauthorized,
            AppConstants.Manager.ProjectNotOwned);
    }

    [Fact]
    public void NotFound_Returns404WithMessage()
    {
        var result = ControllerExceptionHandler.NotFound(AppConstants.Allocations.NotFound);

        AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound,
            AppConstants.Allocations.NotFound);
    }

    [Fact]
    public void Conflict_Returns409WithMessage()
    {
        var result = ControllerExceptionHandler.Conflict(AppConstants.Messages.ConcurrencyConflict);

        AssertErrorResult(
            result,
            StatusCodes.Status409Conflict,
            AppConstants.ErrorCodes.Conflict,
            AppConstants.Messages.ConcurrencyConflict);
    }

    [Fact]
    public void SessionExpired_Returns401WithSessionExpiredCode()
    {
        var result = ControllerExceptionHandler.SessionExpired();

        AssertErrorResult(
            result,
            StatusCodes.Status401Unauthorized,
            AppConstants.ErrorCodes.SessionExpired,
            AppConstants.Messages.SessionExpired);
    }

    [Fact]
    public void InternalError_Returns500WithInternalErrorMessage()
    {
        var result = ControllerExceptionHandler.InternalError();

        AssertErrorResult(
            result,
            StatusCodes.Status500InternalServerError,
            AppConstants.ErrorCodes.InternalError,
            AppConstants.Messages.InternalError);
    }

    private static void AssertErrorResult(
        IActionResult result,
        int statusCode,
        string code,
        string message)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(statusCode, objectResult.StatusCode);

        var error = Assert.IsType<ApiErrorResponse>(objectResult.Value);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }
}
