using Prm.Api.Configuration;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;

namespace Prm.Api.DependencyInjection;

public static class EmailNotificationExtensions
{
    public static IServiceCollection AddEmailNotificationServices(this IServiceCollection services)
    {
        services
            .AddOptions<BrevoOptions>()
            .BindConfiguration(AppConstants.Configuration.BrevoSection)
            .ValidateDataAnnotations()
            .Validate(IsValidWhenEnabled, AppConstants.Messages.BrevoConfigurationInvalid);

        services.AddScoped<IEmailNotificationService, BrevoEmailNotificationService>();

        return services;
    }

    private static bool IsValidWhenEnabled(BrevoOptions options) =>
        !options.Enabled
        || (!string.IsNullOrWhiteSpace(options.SenderEmail)
            && !string.IsNullOrWhiteSpace(options.SmtpLogin)
            && !string.IsNullOrWhiteSpace(options.SmtpKey));
}
