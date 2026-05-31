using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Common.Constants;
using Prm.Common.Models.Auth;
using UserManagement.Services.Interfaces;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class AuthController(IAuthService _authService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Login)]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _authService.Login(request, cancellationToken);
            return Ok(result);
        });

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Refresh)]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(
            async () =>
            {
                var result = await _authService.Refresh(request, cancellationToken);
                return Ok(result);
            },
            treatUnauthorizedAsSessionExpired: true);

    [Authorize]
    [HttpPost(ApiRoutes.Auth.ChangePassword)]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(
            async () =>
            {
                var result = await _authService.ChangePassword(request, cancellationToken);
                return Ok(result);
            },
            treatUnauthorizedAsSessionExpired: true);
}
