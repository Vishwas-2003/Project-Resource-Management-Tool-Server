using Prm.Common.Models.Employees;

namespace Prm.Api.Services.Interfaces;

public interface ISystemConfigurationService
{
    Task<bool> Update(
        int configurationId,
        string value,
        CancellationToken cancellationToken = default);
}
