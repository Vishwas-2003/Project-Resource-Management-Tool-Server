using Prm.Common.Models.Users;

namespace Prm.Api.Services.Interfaces;

public interface IUserService
{
    Task<int> Add(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserListResult> GetUsers(CancellationToken cancellationToken = default);
    Task<bool> Reactivate(int userId, CancellationToken cancellationToken = default);
    Task<bool> ResetPassword(ResetUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> Deactivate(UserLookupRequest request, CancellationToken cancellationToken = default);
}
