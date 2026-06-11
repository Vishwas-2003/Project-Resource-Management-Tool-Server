using Prm.Common.Models.Resources;
using Prm.Common.Models.Manager;

namespace Prm.Api.Services.Interfaces;

public interface IResourceService
{
    Task<ResourceListResult> GetResources(ResourceFilter filter, CancellationToken cancellationToken = default);
    Task<bool> AssignManager(AssignManagerRequest request, CancellationToken cancellationToken = default);
    Task<bool> Update(int resourceUserId, UpdateResourceRequest request, CancellationToken cancellationToken = default);
    Task<bool> Deactivate(int resourceUserId, CancellationToken cancellationToken = default);
    Task<ResourceDetailResponse> GetDetail(int resourceUserId, CancellationToken cancellationToken = default);
    Task<ResourceUtilizationResponse> GetUtilization(int resourceUserId, CancellationToken cancellationToken = default);
}
