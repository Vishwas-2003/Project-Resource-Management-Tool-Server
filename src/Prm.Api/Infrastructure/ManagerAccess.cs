using Prm.Common.Constants;
using Prm.Data.Audit;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Infrastructure;

public sealed class ManagerAccess(
    ICurrentUserService _currentUserService,
    IUserRepository _userRepository)
{
    public int GetCurrentUserId()
    {
        var userId = _currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.UserNotAuthenticated);
        }

        return userId.Value;
    }

    public async Task<int> GetCurrentManagerUserId(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var manager = await _userRepository.GetActiveManagerById(userId, cancellationToken);
        if (manager is null)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }

        return userId;
    }
}
