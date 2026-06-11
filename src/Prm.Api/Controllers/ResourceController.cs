using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Resources;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class ResourceController(IResourceService _resourceService) : ApiControllerBase
{
    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Resources.GetResources)]
    public Task<IActionResult> GetResources([FromBody] ResourceFilter filter, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _resourceService.GetResources(filter, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Resources.AssignManager)]
    public Task<IActionResult> AssignManager(
        [FromBody] AssignManagerRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _resourceService.AssignManager(request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPut(ApiRoutes.Resources.Update)]
    public Task<IActionResult> Update(
        int resourceUserId,
        [FromBody] UpdateResourceRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _resourceService.Update(resourceUserId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Resources.Deactivate)]
    public Task<IActionResult> Deactivate(int resourceUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _resourceService.Deactivate(resourceUserId, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = $"{nameof(RoleNameEnum.Admin)},{nameof(RoleNameEnum.Manager)}")]
    [HttpGet(ApiRoutes.Resources.GetDetail)]
    public Task<IActionResult> GetDetail(int resourceUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _resourceService.GetDetail(resourceUserId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Resources.GetUtilization)]
    public Task<IActionResult> GetUtilization(int resourceUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _resourceService.GetUtilization(resourceUserId, cancellationToken);
            return Ok(result);
        });
}
