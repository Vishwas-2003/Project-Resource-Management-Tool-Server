using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default);
}
