using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class RefreshTokenRepository(AppDbContext dbContext)
    : CrudBaseRepository<RefreshToken, int>(dbContext), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenWithUser(string token, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public async Task RemoveByUserId(int userId, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var token in existing)
        {
            Remove(token);
        }
    }
}
