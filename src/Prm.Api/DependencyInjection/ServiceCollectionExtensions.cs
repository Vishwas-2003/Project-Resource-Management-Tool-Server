using Microsoft.AspNetCore.Identity;
using Prm.Api.Configuration;
using Prm.Common.Constants;
using Prm.Data.Entities;
using Prm.Data.Repositories;
using Prm.Data.Repositories.Interfaces;
using UserManagement.Configuration;
using UserManagement.Services;
using UserManagement.Services.Interfaces;

namespace Prm.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        return services;
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;

        services.AddAutoMapper(
            typeof(ServiceCollectionExtensions).Assembly,
            typeof(Prm.Data.SeedData).Assembly);

        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(AppConstants.Configuration.JwtSection)
            .ValidateDataAnnotations()
            .Validate(
                x => !string.IsNullOrWhiteSpace(x.Secret)
                    && !string.IsNullOrWhiteSpace(x.Issuer)
                    && !string.IsNullOrWhiteSpace(x.Audience),
                AppConstants.Messages.JwtConfigurationInvalid);

        services
            .AddOptions<BootstrapAdminOptions>()
            .BindConfiguration(AppConstants.Configuration.BootstrapAdminSection)
            .ValidateDataAnnotations();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
