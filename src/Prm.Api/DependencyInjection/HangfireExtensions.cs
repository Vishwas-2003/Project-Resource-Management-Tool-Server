using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Api.Infrastructure;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;

namespace Prm.Api.DependencyInjection;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AppConstants.Configuration.DefaultConnection)
            ?? throw new InvalidOperationException(
                $"Connection string '{AppConstants.Configuration.DefaultConnection}' is missing.");

        services
            .AddOptions<HangfireOptions>()
            .BindConfiguration(HangfireOptions.Section)
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DashboardUsername)
                    && !string.IsNullOrWhiteSpace(options.DashboardPassword),
                "Hangfire dashboard credentials are missing.");

        services.AddSingleton<HangfireDashboardAuthorizationFilter>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
            }));

        services.AddHangfireServer();

        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IProjectRiskAlertService, ProjectRiskAlertService>();
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<IHangfireJobScheduler, HangfireJobScheduler>();

        return services;
    }
}
