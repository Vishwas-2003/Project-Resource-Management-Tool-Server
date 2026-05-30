using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IRefreshTokenRepository : ICrudBaseRepository<RefreshToken, int>
{
    Task<RefreshToken?> GetByTokenWithUser(string token, CancellationToken cancellationToken = default);
    Task RemoveByUserId(int userId, CancellationToken cancellationToken = default);
}
