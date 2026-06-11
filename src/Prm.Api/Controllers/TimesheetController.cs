using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Timesheets;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class TimesheetController(
    ITimesheetService _timesheetService,
    ManagerAccess _managerAccess) : ApiControllerBase
{
    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.ActivityTags)]
    public Task<IActionResult> GetActivityTags(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _timesheetService.GetActivityTags(cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.Reminder)]
    public Task<IActionResult> GetReminder(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.GetMissingReminder(userId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.WeekAllocations)]
    public Task<IActionResult> GetWeekAllocations(
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.GetWeekAllocations(userId, weekStart, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpPost(ApiRoutes.Timesheets.Submit)]
    public Task<IActionResult> Submit(
        [FromBody] SubmitTimesheetRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.SubmitTimesheet(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.MyTimesheets)]
    public Task<IActionResult> GetMyTimesheets(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.GetMyTimesheets(userId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.MyTimesheetDetail)]
    public Task<IActionResult> GetMyTimesheetDetail(DateOnly weekStart, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.GetMyTimesheetDetail(userId, weekStart, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Employee))]
    [HttpGet(ApiRoutes.Timesheets.MyAllocations)]
    public Task<IActionResult> GetMyAllocations(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var userId = _managerAccess.GetCurrentUserId();
            var result = await _timesheetService.GetMyAllocations(userId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Timesheets.Team)]
    public Task<IActionResult> GetTeamTimesheets(
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerUserId = await _managerAccess.GetCurrentManagerUserId(cancellationToken);
            var result = await _timesheetService.GetTeamTimesheets(managerUserId, weekStart, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Timesheets.TeamEmployeeDetail)]
    public Task<IActionResult> GetEmployeeTimesheetDetail(
        int employeeUserId,
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var managerUserId = await _managerAccess.GetCurrentManagerUserId(cancellationToken);
            var result = await _timesheetService.GetEmployeeTimesheetDetail(
                managerUserId,
                employeeUserId,
                weekStart,
                cancellationToken);
            return Ok(result);
        });
}
