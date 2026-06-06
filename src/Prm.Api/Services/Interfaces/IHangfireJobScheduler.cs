namespace Prm.Api.Services.Interfaces;

public interface IHangfireJobScheduler
{
    Task RegisterRecurringJobsAsync(CancellationToken cancellationToken = default);

    void RescheduleScheduler(int intervalMinutes);
}
