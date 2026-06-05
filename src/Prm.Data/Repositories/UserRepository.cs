using Microsoft.EntityFrameworkCore;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class UserRepository(AppDbContext _dbContext)
    : CrudBaseRepository<User, int>(_dbContext), IUserRepository
{
    public Task<User?> GetByUsername(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == normalized, cancellationToken);
    }

    public Task<User?> GetByIdWithRole(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<User?> GetByIdWithRoleAndEmployee(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Role)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<User?> GetActiveManagerById(int userId, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Id == userId
                    && x.IsActive
                    && x.RoleId == (int)RoleNameEnum.Manager,
                cancellationToken);

    public async Task<IReadOnlyList<User>> GetUsers(CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Role)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByUsername(string username, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Username == username.Trim(), cancellationToken);

    public Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(x => x.Email == email.Trim(), cancellationToken);

    public async Task<bool> IsLastActiveAdmin(User user, CancellationToken cancellationToken)
    {
        var admins = await DbSet
            .Where(user => user.IsActive && user.RoleId == (int)RoleNameEnum.Admin)
            .ToListAsync();
        
        if (admins.Count == 1 && admins.Any(admin => admin.Id == user.Id))
        {
            return true;
        }

        return false;
    }
}
