using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class RoleRepository(AppDbContext _dbContext) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) =>
        _dbContext.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);
}
