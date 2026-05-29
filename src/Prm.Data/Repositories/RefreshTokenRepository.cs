using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class RefreshTokenRepository(AppDbContext _dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public async Task RemoveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _dbContext.RefreshTokens.RemoveRange(existing);
        }
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
