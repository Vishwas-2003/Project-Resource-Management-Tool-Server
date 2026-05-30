using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Models;
using Prm.Api.Models.Employees;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class EmployeeController(IEmployeeService _employeeService, IMapper _mapper) : ApiControllerBase
{
    [HttpPost(ApiRoutes.Employees.GetEmployees)]
    public Task<IActionResult> GetEmployees([FromBody] EmployeeFilter filter, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var result = await _employeeService.GetEmployees(filter, cancellationToken);
            return Ok(_mapper.Map<GetEmployeesResponse>(result));
        });

    [HttpPost(ApiRoutes.Employees.AddEmployee)]
    public Task<IActionResult> Add([FromBody] AddEmployeeRequest request, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var id = await _employeeService.Add(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new CreatedIdResponse { Id = id });
        });

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

    [HttpPost(ApiRoutes.Employees.Deactivate)]
    public Task<IActionResult> Deactivate(int employeeId, CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _employeeService.Deactivate(employeeId, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });
}
