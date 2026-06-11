using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Employees;

namespace Prm.Api.Controllers;

[ApiController]
[Route(ApiRoutes.BaseApi)]
public class EmployeeController(IEmployeeService _employeeService) : ApiControllerBase
{
    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Employees.GetEmployees)]
    public Task<IActionResult> GetEmployees([FromBody] EmployeeFilter filter, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetEmployees(filter, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Employees.AssignManager)]
    public Task<IActionResult> AssignManager(
        [FromBody] AssignManagerRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.AssignManager(request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPut(ApiRoutes.Employees.Update)]
    public Task<IActionResult> Update(
        int employeeUserId,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.Update(employeeUserId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Employees.Deactivate)]
    public Task<IActionResult> Deactivate(int employeeUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.Deactivate(employeeUserId, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = $"{nameof(RoleNameEnum.Admin)},{nameof(RoleNameEnum.Manager)}")]
    [HttpGet(ApiRoutes.Employees.GetDetail)]
    public Task<IActionResult> GetDetail(int employeeUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetDetail(employeeUserId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Employees.GetUtilization)]
    public Task<IActionResult> GetUtilization(int employeeUserId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetUtilization(employeeUserId, cancellationToken);
            return Ok(result);
        });
}
