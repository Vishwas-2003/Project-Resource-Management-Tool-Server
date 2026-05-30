using Microsoft.EntityFrameworkCore;
using Prm.Data.Entities;
using Prm.Data.Persistence;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Data.Repositories;

public class EmployeeSkillRepository(AppDbContext dbContext)
    : CrudBaseRepository<EmployeeSkill, EmployeeSkillKey>(dbContext), IEmployeeSkillRepository
{
    public override Task<EmployeeSkill?> GetById(EmployeeSkillKey id, CancellationToken cancellationToken = default) =>
        DbSet
            .Include(x => x.Skill)
            .Include(x => x.Employee)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(
                x => x.EmployeeId == id.EmployeeId && x.SkillId == id.SkillId,
                cancellationToken);

    public override async Task<bool> Exists(EmployeeSkillKey id, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            x => x.EmployeeId == id.EmployeeId && x.SkillId == id.SkillId,
            cancellationToken);

    public async Task<IReadOnlyList<EmployeeSkill>> GetByEmployeeId(
        int employeeId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(x => x.Skill)
            .Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Skill.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> Exists(int employeeId, int skillId, CancellationToken cancellationToken = default) =>
        Exists(new EmployeeSkillKey(employeeId, skillId), cancellationToken);
}
