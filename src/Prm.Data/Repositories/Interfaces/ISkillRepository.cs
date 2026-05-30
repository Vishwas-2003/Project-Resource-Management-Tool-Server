using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface ISkillRepository : ICrudBaseRepository<Skill, int>
{
    Task<Skill?> GetByName(string name, CancellationToken cancellationToken = default);
}
