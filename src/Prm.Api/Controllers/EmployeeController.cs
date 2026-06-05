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
    [HttpPost(ApiRoutes.Employees.AddEmployee)]
    public Task<IActionResult> Add([FromBody] AddEmployeeRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _employeeService.Add(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPut(ApiRoutes.Employees.Update)]
    public Task<IActionResult> Update(
        int employeeId,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.Update(employeeId, request, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = nameof(RoleNameEnum.Admin))]
    [HttpPost(ApiRoutes.Employees.Deactivate)]
    public Task<IActionResult> Deactivate(int employeeId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.Deactivate(employeeId, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });

    [Authorize(Roles = $"{nameof(RoleNameEnum.Admin)},{nameof(RoleNameEnum.Manager)}")]
    [HttpGet(ApiRoutes.Employees.GetDetail)]
    public Task<IActionResult> GetDetail(int employeeId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetDetail(employeeId, cancellationToken);
            return Ok(result);
        });

    [Authorize(Roles = nameof(RoleNameEnum.Manager))]
    [HttpGet(ApiRoutes.Employees.GetUtilization)]
    public Task<IActionResult> GetUtilization(int employeeId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetUtilization(employeeId, cancellationToken);
            return Ok(result);
        });
}
