namespace Prm.Api.Services.Interfaces;

public interface IProjectRiskAlertService
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
