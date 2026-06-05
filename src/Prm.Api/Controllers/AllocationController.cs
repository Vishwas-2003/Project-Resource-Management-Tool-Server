using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Manager))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class AllocationController(
    IAllocationService _allocationService,
    ManagerAccess _managerAccess) : ApiControllerBase
{
    [HttpPost(ApiRoutes.Allocations.Create)]
    public Task<IActionResult> Create(
        [FromBody] CreateAllocationRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerUserId = await _managerAccess.GetCurrentManagerUserId(cancellationToken);
            var result = await _allocationService.Create(request, managerUserId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        });

    [HttpPost(ApiRoutes.Allocations.End)]
    public Task<IActionResult> End(int allocationId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerUserId = await _managerAccess.GetCurrentManagerUserId(cancellationToken);
            var result = await _allocationService.End(allocationId, managerUserId, cancellationToken);
            return Ok(result);
        });

    [HttpGet(ApiRoutes.Allocations.GetByProject)]
    public Task<IActionResult> GetByProject(int projectId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerUserId = await _managerAccess.GetCurrentManagerUserId(cancellationToken);
            var result = await _allocationService.GetByProjectId(
                projectId,
                managerUserId,
                cancellationToken);
            return Ok(result);
        });
}
