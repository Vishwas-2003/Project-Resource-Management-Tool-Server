using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class UserRepository(AppDbContext _dbContext) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim();
        return _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Username == normalized,
                cancellationToken);
    }

    public Task<User?> GetByIdWithRoleAsync(int userId, CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
