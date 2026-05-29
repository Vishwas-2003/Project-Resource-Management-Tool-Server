using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenWithUserAsync(string token, CancellationToken cancellationToken = default);
    Task RemoveByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
