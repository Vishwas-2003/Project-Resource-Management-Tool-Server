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
    [HttpGet(ApiRoutes.Skills.GetForEmployee)]
    public Task<IActionResult> GetForEmployee(int employeeId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _skillService.GetForEmployee(employeeId, cancellationToken);
            return Ok(result);
        });

    [HttpPost(ApiRoutes.Skills.Add)]
    public Task<IActionResult> Add(
        int employeeId,
        [FromBody] AddEmployeeSkillRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _skillService.Add(employeeId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [HttpPut(ApiRoutes.Skills.Update)]
    public Task<IActionResult> Update(
        int employeeId,
        int skillId,
        [FromBody] UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _skillService.Update(employeeId, skillId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [HttpDelete(ApiRoutes.Skills.Remove)]
    public Task<IActionResult> Remove(int employeeId, int skillId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            await _skillService.Remove(employeeId, skillId, cancellationToken);
            return Ok(new { message = AppConstants.Skills.SkillRemoved });
        });
}
