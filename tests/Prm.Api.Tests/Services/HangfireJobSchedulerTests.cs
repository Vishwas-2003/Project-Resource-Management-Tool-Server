using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Options;
using Moq;
using Prm.Api.Configuration;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Enums;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Tests.Services;

public class HangfireJobSchedulerTests
{
    private readonly Mock<IRecurringJobManager> _recurringJobManager = new();
    private readonly Mock<ISystemConfigurationRepository> _systemConfigurationRepository = new();
    private readonly HangfireOptions _hangfireOptions = new()
    {
        RecurringJobId = "prm-scheduler",
        ProjectRiskAlertRecurringJobId = "prm-project-risk-alert",
        DefaultSchedulerIntervalMinutes = 60,
        DashboardUsername = "admin",
        DashboardPassword = "password",
    };

    [Fact]
    public async Task RegisterRecurringJobsAsync_WhenConfiguredIntervalExists_ReschedulesWithConfiguredValue()
    {
        _systemConfigurationRepository
            .Setup(x => x.GetById((int)ConfigurationOptionEnum.SchedulerInterval, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.SchedulerInterval, "30"));

        var sut = CreateSut();
        await sut.RegisterRecurringJobsAsync();

        _recurringJobManager.Verify(
            x => x.AddOrUpdate(
                _hangfireOptions.RecurringJobId,
                It.IsAny<Job>(),
                Cron.MinuteInterval(30),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
        _recurringJobManager.Verify(
            x => x.AddOrUpdate(
                _hangfireOptions.ProjectRiskAlertRecurringJobId,
                It.IsAny<Job>(),
                Cron.Hourly(),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterRecurringJobsAsync_WhenConfigurationMissing_UsesDefaultInterval()
    {
        _systemConfigurationRepository
            .Setup(x => x.GetById((int)ConfigurationOptionEnum.SchedulerInterval, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prm.Data.Entities.SystemConfiguration?)null);

        var sut = CreateSut();
        await sut.RegisterRecurringJobsAsync();

        _recurringJobManager.Verify(
            x => x.AddOrUpdate(
                _hangfireOptions.RecurringJobId,
                It.IsAny<Job>(),
                Cron.MinuteInterval(_hangfireOptions.DefaultSchedulerIntervalMinutes),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
        _recurringJobManager.Verify(
            x => x.AddOrUpdate(
                _hangfireOptions.ProjectRiskAlertRecurringJobId,
                It.IsAny<Job>(),
                Cron.Hourly(),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    [Fact]
    public void RescheduleScheduler_CallsAddOrUpdateWithInterval()
    {
        var sut = CreateSut();
        sut.RescheduleScheduler(15);

        _recurringJobManager.Verify(
            x => x.AddOrUpdate(
                _hangfireOptions.RecurringJobId,
                It.IsAny<Job>(),
                Cron.MinuteInterval(15),
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }

    private HangfireJobScheduler CreateSut() =>
        new(
            Options.Create(_hangfireOptions),
            _systemConfigurationRepository.Object,
            _recurringJobManager.Object);
}
