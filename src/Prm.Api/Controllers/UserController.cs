using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Models;
using Prm.Api.Models.Users;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Users;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class UserController(IUserService _userService, IMapper _mapper) : ApiControllerBase
{
    [HttpPost(ApiRoutes.Users.Add)]
    public Task<IActionResult> Add([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _userService.Add(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [HttpPost(ApiRoutes.Users.GetUsers)]
    public Task<IActionResult> GetUsers(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _userService.GetUsers(cancellationToken);
            return Ok(_mapper.Map<GetUsersResponse>(result));
        });

    [HttpPost(ApiRoutes.Users.Reactivate)]
    public Task<IActionResult> Reactivate(int userId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _userService.Reactivate(userId, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [HttpPost(ApiRoutes.Users.ResetPassword)]
    public Task<IActionResult> ResetPassword(
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _userService.ResetPassword(request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [HttpPost(ApiRoutes.Users.Deactivate)]
    public Task<IActionResult> Deactivate(
        [FromBody] UserLookupRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _userService.Deactivate(request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });
}
