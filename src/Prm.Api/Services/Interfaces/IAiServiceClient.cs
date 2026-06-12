using Prm.Api.Models.Ai;

namespace Prm.Api.Services.Interfaces;

public interface IAiServiceClient
{
    Task<RiskSummaryResponse> GetRiskSummaryAsync(int projectId, CancellationToken cancellationToken = default);

    Task<TeamBuilderResponse> BuildTeamAsync(string query, CancellationToken cancellationToken = default);
}
