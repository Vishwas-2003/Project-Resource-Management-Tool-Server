using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Audit;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Infrastructure;

public sealed class ManagerAccess(
    ICurrentUserService _currentUserService,
    IEmployeeRepository _employeeRepository)
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

    public async Task<int> GetCurrentManagerEmployeeId(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var employee = await _employeeRepository.GetEmployeeByUserId(userId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }

        return employee.Id;
    }
}
