using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IUserRepository : ICrudBaseRepository<User, int>
{
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRole(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRoleAndEmployee(int userId, CancellationToken cancellationToken = default);
}
