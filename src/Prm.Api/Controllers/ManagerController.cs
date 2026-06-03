using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Manager))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class ManagerController(
    IManagerService _managerService,
    ManagerAccess _managerAccess) : ApiControllerBase
{
    [HttpGet(ApiRoutes.Manager.ResourceDashboard)]
    public Task<IActionResult> GetResourceDashboard(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _managerService.GetResourceDashboard(userId, cancellationToken);
            return Ok(result);
        });
}
