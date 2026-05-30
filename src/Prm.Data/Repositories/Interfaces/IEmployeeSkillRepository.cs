using Prm.Data.Entities;

namespace Prm.Data.Repositories.Interfaces;

public interface IEmployeeSkillRepository : ICrudBaseRepository<EmployeeSkill, EmployeeSkillKey>
{
    Task<IReadOnlyList<EmployeeSkill>> GetByEmployeeId(int employeeId, CancellationToken cancellationToken = default);
    Task<bool> Exists(int employeeId, int skillId, CancellationToken cancellationToken = default);
}
