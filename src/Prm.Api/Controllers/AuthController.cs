using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Models.Auth;
using Prm.Common.Constants;
using Prm.Common.Models.Auth;
using UserManagement.Services.Interfaces;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class AuthController(IAuthService _authService, IMapper _mapper) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Login)]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(_mapper.Map<LoginResponse>(result));
        });

    [AllowAnonymous]
    [HttpPost(ApiRoutes.Auth.Refresh)]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(
            async () =>
            {
                var result = await _authService.RefreshAsync(request, cancellationToken);
                return Ok(_mapper.Map<LoginResponse>(result));
            },
            treatUnauthorizedAsSessionExpired: true);
}
