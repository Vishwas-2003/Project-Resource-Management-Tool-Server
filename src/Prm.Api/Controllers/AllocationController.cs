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
            var managerEmployeeId = await _managerAccess.GetCurrentManagerEmployeeId(cancellationToken);
            var result = await _allocationService.Create(request, managerEmployeeId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        });

    [HttpPost(ApiRoutes.Allocations.End)]
    public Task<IActionResult> End(int allocationId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerEmployeeId = await _managerAccess.GetCurrentManagerEmployeeId(cancellationToken);
            var result = await _allocationService.End(allocationId, managerEmployeeId, cancellationToken);
            return Ok(result);
        });

    [HttpGet(ApiRoutes.Allocations.GetByProject)]
    public Task<IActionResult> GetByProject(int projectId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerEmployeeId = await _managerAccess.GetCurrentManagerEmployeeId(cancellationToken);
            var result = await _allocationService.GetByProjectId(
                projectId,
                managerEmployeeId,
                cancellationToken);
            return Ok(result);
        });
}
