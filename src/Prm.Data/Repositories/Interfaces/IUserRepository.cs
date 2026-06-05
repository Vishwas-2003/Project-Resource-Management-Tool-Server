using Prm.Common.Models.Users;
using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IUserRepository : ICrudBaseRepository<User, int>
{
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRole(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRoleAndEmployee(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetActiveManagerById(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetUsers(CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default);
    Task<bool> IsLastActiveAdmin(User user, CancellationToken cancellationToken);
}
