using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class SkillRepository(AppDbContext dbContext)
    : CrudBaseRepository<Skill, int>(dbContext), ISkillRepository
{
    public Task<Skill?> GetByName(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();
        return DbSet.FirstOrDefaultAsync(x => x.Name == normalized, cancellationToken);
    }
}
