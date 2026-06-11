using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Skills;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class SkillController(ISkillService _skillService) : ApiControllerBase
{
    [HttpGet(ApiRoutes.Skills.GetForResource)]
    public Task<IActionResult> GetForResource(int resourceUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _skillService.GetForResource(resourceUserId, cancellationToken);
            return Ok(result);
        });

    [HttpPost(ApiRoutes.Skills.Add)]
    public Task<IActionResult> Add(
        int resourceUserId,
        [FromBody] AddResourceSkillRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _skillService.Add(resourceUserId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [HttpPut(ApiRoutes.Skills.Update)]
    public Task<IActionResult> Update(
        int resourceUserId,
        int skillId,
        [FromBody] UpdateResourceSkillRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _skillService.Update(resourceUserId, skillId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [HttpDelete(ApiRoutes.Skills.Remove)]
    public Task<IActionResult> Remove(int resourceUserId, int skillId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            await _skillService.Remove(resourceUserId, skillId, cancellationToken);
            return Ok(new { message = AppConstants.Skills.SkillRemoved });
        });
}
