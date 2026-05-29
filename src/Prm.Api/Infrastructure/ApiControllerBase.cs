using Microsoft.AspNetCore.Mvc;

namespace Prm.Api.Infrastructure;

public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> ExecuteResultAsync(
        Func<Task<IActionResult>> action,
        bool treatUnauthorizedAsSessionExpired = false)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            return ControllerExceptionHandler.Handle(ex, treatUnauthorizedAsSessionExpired);
        }
    }
}
