using System.Security.Claims;
using Prm.Data.Audit;

namespace Prm.Api.Infrastructure;

public sealed class CurrentUserService(IHttpContextAccessor _httpContextAccessor) : ICurrentUserService
{
    public int? GetUserId()
    {
        var userIdValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
