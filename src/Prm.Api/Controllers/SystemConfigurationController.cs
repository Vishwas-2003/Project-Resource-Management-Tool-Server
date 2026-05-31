using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Models;
using Prm.Api.Models.Employees;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class SystemConfigurationController(ISystemConfigurationService _systemConfigurationService, IMapper _mapper) : ApiControllerBase
{
    [HttpPut(ApiRoutes.SystemConfiguration.Update)]
    public Task<IActionResult> Update(
        int configurationId,
        [FromBody] string value,
        CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var updated = await _systemConfigurationService.Update(configurationId, value, cancellationToken);
            return Ok(new UpdatedResponse { Updated = updated });
        });
}
