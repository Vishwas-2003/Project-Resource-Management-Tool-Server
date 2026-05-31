using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Projects;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class ProjectController(IProjectService _projectService) : ApiControllerBase
{
    [HttpPost(ApiRoutes.Projects.Add)]
    public Task<IActionResult> Add([FromBody] CreateProjectRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _projectService.Add(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [HttpPost(ApiRoutes.Projects.GetProjects)]
    public Task<IActionResult> GetProjects(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _projectService.GetProjects(cancellationToken);
            return Ok(result);
        });

    [HttpPut(ApiRoutes.Projects.Update)]
    public Task<IActionResult> Update(
        int projectId,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _projectService.Update(projectId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });
}
