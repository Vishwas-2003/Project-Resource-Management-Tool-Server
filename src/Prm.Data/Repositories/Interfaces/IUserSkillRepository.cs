using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IUserSkillRepository : ICrudBaseRepository<UserSkill, UserSkillKey>
{
    Task<IReadOnlyList<UserSkill>> GetByUserId(int userId, CancellationToken cancellationToken = default);

    Task<bool> Exists(int userId, int skillId, CancellationToken cancellationToken = default);
}
