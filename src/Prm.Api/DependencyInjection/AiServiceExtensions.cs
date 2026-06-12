using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;

namespace Prm.Api.DependencyInjection;

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiServiceClient(this IServiceCollection services)
    {
        services
            .AddOptions<AiServiceOptions>()
            .BindConfiguration(AppConstants.Configuration.AiSection)
            .ValidateDataAnnotations();

        services.AddHttpClient<IAiServiceClient, AiServiceClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        });

        return services;
    }
}
