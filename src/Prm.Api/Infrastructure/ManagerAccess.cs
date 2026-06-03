using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Audit;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Infrastructure;

public sealed class ManagerAccess(
    ICurrentUserService currentUserService,
    IEmployeeRepository employeeRepository)
{
    public int GetCurrentUserId()
    {
        var userId = currentUserService.GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException(AppConstants.Auth.UserNotAuthenticated);
        }

        return userId.Value;
    }

    public async Task<int> GetCurrentManagerEmployeeId(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var employee = await employeeRepository.GetEmployeeByUserId(userId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }

        return employee.Id;
    }
}
