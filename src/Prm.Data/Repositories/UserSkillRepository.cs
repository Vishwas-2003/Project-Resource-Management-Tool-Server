using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class UserSkillRepository(AppDbContext _dbContext)
    : CrudBaseRepository<UserSkill, UserSkillKey>(_dbContext), IUserSkillRepository
{
    public override Task<UserSkill?> GetById(UserSkillKey id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Skill)
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.UserId == id.UserId && x.SkillId == id.SkillId,
                cancellationToken);

    public override async Task<bool> Exists(UserSkillKey id, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            x => x.UserId == id.UserId && x.SkillId == id.SkillId,
            cancellationToken);

    public async Task<IReadOnlyList<UserSkill>> GetByUserId(
        int userId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Skill)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Skill.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> Exists(int userId, int skillId, CancellationToken cancellationToken = default) =>
        Exists(new UserSkillKey(userId, skillId), cancellationToken);
}
