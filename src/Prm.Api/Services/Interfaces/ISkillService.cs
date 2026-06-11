using Prm.Common.Models.Skills;

namespace Prm.Api.Services.Interfaces;

public interface ISkillService
{
    Task<EmployeeSkillsResult> GetForEmployee(int employeeUserId, CancellationToken cancellationToken = default);
    Task<int> Add(int employeeUserId, AddEmployeeSkillRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(
        int employeeUserId,
        int skillId,
        UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken = default);
    Task Remove(int employeeUserId, int skillId, CancellationToken cancellationToken = default);
}
