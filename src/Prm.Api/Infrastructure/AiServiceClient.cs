using System.Net.Http.Json;
using System.Text.Json;
using Prm.Api.Models.Ai;
using Prm.Api.Services.Interfaces;

namespace Prm.Api.Infrastructure;

public class AiServiceClient(HttpClient _httpClient) : IAiServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<RiskSummaryResponse> GetRiskSummaryAsync(
        int projectId,
        CancellationToken cancellationToken = default) =>
        PostAsync<RiskSummaryRequest, RiskSummaryResponse>(
            "ai/risk-summary",
            new RiskSummaryRequest { ProjectId = projectId },
            cancellationToken);

    public Task<TeamBuilderResponse> BuildTeamAsync(
        string query,
        CancellationToken cancellationToken = default) =>
        PostAsync<TeamBuilderRequest, TeamBuilderResponse>(
            "ai/team-builder",
            new TeamBuilderRequest { Query = query },
            cancellationToken);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            relativeUrl,
            request,
            JsonOptions,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
            return payload ?? throw new InvalidOperationException("AiService returned an empty response body.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"AiService request to '{relativeUrl}' failed with status {(int)response.StatusCode}: {body}");
    }
}
