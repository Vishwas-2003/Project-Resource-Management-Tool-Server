using Prm.Common.Models.Skills;

namespace Prm.Api.Services.Interfaces;

public interface ISkillService
{
    Task<EmployeeSkillsResult> GetForEmployee(int employeeId, CancellationToken cancellationToken = default);
    Task<int> Add(int employeeId, AddEmployeeSkillRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken = default);
    Task Remove(int employeeId, int skillId, CancellationToken cancellationToken = default);
}
