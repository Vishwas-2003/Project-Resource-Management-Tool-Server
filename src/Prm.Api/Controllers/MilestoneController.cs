using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Milestones;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class MilestoneController(IMilestoneService _milestoneService) : ApiControllerBase
{
    [HttpGet(ApiRoutes.Milestones.GetByProject)]
    public Task<IActionResult> GetByProject(int projectId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _milestoneService.GetByProjectId(projectId, cancellationToken);
            return Ok(result);
        });

    [HttpPost(ApiRoutes.Milestones.Add)]
    public Task<IActionResult> Add(
        int projectId,
        [FromBody] AddMilestoneRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _milestoneService.Add(projectId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [HttpPut(ApiRoutes.Milestones.Update)]
    public Task<IActionResult> Update(
        int projectId,
        int milestoneId,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _milestoneService.Update(projectId, milestoneId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });
}
