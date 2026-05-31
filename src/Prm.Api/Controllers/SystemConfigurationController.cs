using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;

namespace Prm.Api.Controllers;

[Authorize(Roles = nameof(RoleNameEnum.Admin))]
[ApiController]
[Route(ApiRoutes.BaseApi)]
public class SystemConfigurationController(ISystemConfigurationService _systemConfigurationService) : ApiControllerBase
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

    [HttpGet]
    public Task<IActionResult> Get(CancellationToken cancellationToken) =>
        ExecuteResultAsync(async () =>
        {
            var configurations = await _systemConfigurationService.GetAllConfigurations(cancellationToken);
            return Ok(configurations);
        });
}
