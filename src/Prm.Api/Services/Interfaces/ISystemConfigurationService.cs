using Prm.Common.Models.SystemConfigurations;

namespace Prm.Api.Services.Interfaces;

public interface ISystemConfigurationService
{
    Task<IReadOnlyList<SystemConfigurationResponse>> GetAllConfigurations(CancellationToken cancellationToken = default);
    Task<bool> Update(
        int configurationId,
        string value,
        CancellationToken cancellationToken = default);
}
