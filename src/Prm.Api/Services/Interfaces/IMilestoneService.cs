using Prm.Common.Models.Milestones;

namespace Prm.Api.Services.Interfaces;

public interface IMilestoneService
{
    Task<ProjectMilestonesResult> GetByProjectId(int projectId, CancellationToken cancellationToken = default);
    Task<int> Add(int projectId, AddMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(
        int projectId,
        int milestoneId,
        UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default);
}
