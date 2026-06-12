using Hangfire;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Api.Services.Interfaces;
using Prm.Common.Enums;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class HangfireJobScheduler(
    IOptions<HangfireOptions> _hangfireOptionsAccessor,
    ISystemConfigurationRepository _systemConfigurationRepository,
    IRecurringJobManager _recurringJobManager) : IHangfireJobScheduler
{
    private readonly HangfireOptions _hangfireOptions = _hangfireOptionsAccessor.Value;

    public async Task RegisterRecurringJobsAsync(CancellationToken cancellationToken = default)
    {
        var intervalMinutes = await ResolveSchedulerIntervalMinutes(cancellationToken);
        RescheduleScheduler(intervalMinutes);
        RegisterProjectRiskAlertJob();
    }

    public void RescheduleScheduler(int intervalMinutes)
    {
        _recurringJobManager.AddOrUpdate<ISchedulerService>(
            _hangfireOptions.RecurringJobId,
            service => service.Execute(CancellationToken.None),
            Cron.MinuteInterval(intervalMinutes));
    }

    private void RegisterProjectRiskAlertJob()
    {
        _recurringJobManager.AddOrUpdate<IProjectRiskAlertService>(
            _hangfireOptions.ProjectRiskAlertRecurringJobId,
            service => service.ExecuteAsync(CancellationToken.None),
            Cron.Daily(_hangfireOptions.ProjectRiskAlertHourUtc));
    }

    private async Task<int> ResolveSchedulerIntervalMinutes(CancellationToken cancellationToken)
    {
        var configuration = await _systemConfigurationRepository.GetById(
            (int)ConfigurationOptionEnum.SchedulerInterval,
            cancellationToken);

        if (configuration is not null
            && !string.IsNullOrWhiteSpace(configuration.Value)
            && int.TryParse(configuration.Value, out var configuredMinutes)
            && configuredMinutes > 0)
        {
            return configuredMinutes;
        }

        return _hangfireOptions.DefaultSchedulerIntervalMinutes;
    }
}
