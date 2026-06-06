namespace Prm.Api.Services.Interfaces;

public interface ISchedulerService
{
    Task Execute(CancellationToken cancellationToken = default);
}
