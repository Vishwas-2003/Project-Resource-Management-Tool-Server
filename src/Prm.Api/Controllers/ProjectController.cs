using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Projects;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class ProjectController(
    IProjectService _projectService,
    ManagerAccess _managerAccess) : ApiControllerBase
{
    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Projects.Add)]
    public Task<IActionResult> Add([FromBody] CreateProjectRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _projectService.Add(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Projects.GetProjects)]
    public Task<IActionResult> GetProjects(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _projectService.GetProjects(cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
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

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Projects.MyProjects)]
    public Task<IActionResult> GetMyProjects(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _projectService.GetMyProjects(userId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Projects.GetDetail)]
    public Task<IActionResult> GetDetail(int projectId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _projectService.GetProjectDetail(projectId, userId, cancellationToken);
            return Ok(result);
        });
}
