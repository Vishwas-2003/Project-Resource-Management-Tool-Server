using Prm.Common.Models.Manager;

namespace Prm.Api.Services.Interfaces;

public interface IManagerService
{
    Task<ResourceDashboardResponse> GetResourceDashboard(
        int managerUserId,
        CancellationToken cancellationToken = default);
}
