using Hangfire;
using Prm.Api.Services.Interfaces;

namespace Prm.Api.Services;

public class BackgroundJobService(IBackgroundJobClient _backgroundJobClient) : IBackgroundJobService
{
    public string EnqueueSchedulerRun() =>
        _backgroundJobClient.Enqueue<ISchedulerService>(service =>
            service.Execute(CancellationToken.None));
}
