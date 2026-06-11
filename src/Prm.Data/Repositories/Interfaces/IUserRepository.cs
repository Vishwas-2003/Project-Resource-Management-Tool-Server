using Prm.Common.Models.Employees;
using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IUserRepository : ICrudBaseRepository<User, int>
{
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRole(int userId, CancellationToken cancellationToken = default);
    Task<User?> GetActiveManagerById(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetUsers(CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default);
    Task<bool> IsLastActiveAdmin(User user, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetEmployeeUsers(EmployeeFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetResourcePoolUsers(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetEmployeeUsersByManagerUserId(
        int managerUserId,
        CancellationToken cancellationToken = default);
    Task<User?> GetEmployeeUserDetailById(int userId, CancellationToken cancellationToken = default);
    Task SetManager(int userId, int managerUserId, CancellationToken cancellationToken = default);
    Task SetCurrentResourceStatus(int userId, int resourceStatusTypeId, CancellationToken cancellationToken = default);
}
