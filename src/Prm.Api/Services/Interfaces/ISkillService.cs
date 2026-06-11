using Prm.Common.Models.Skills;

namespace Prm.Api.Services.Interfaces;

public interface ISkillService
{
    Task<ResourceSkillsResult> GetForResource(int resourceUserId, CancellationToken cancellationToken = default);
    Task<int> Add(int resourceUserId, AddResourceSkillRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(
        int resourceUserId,
        int skillId,
        UpdateResourceSkillRequest request,
        CancellationToken cancellationToken = default);
    Task Remove(int resourceUserId, int skillId, CancellationToken cancellationToken = default);
}
