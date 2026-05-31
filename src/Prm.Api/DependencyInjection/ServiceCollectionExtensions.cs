using Microsoft.AspNetCore.Identity;
using Prm.Api.Configuration;
using Prm.Api.Infrastructure;
using Prm.Common.Constants;
using Prm.Data.Audit;
using Prm.Data.Entities;
using Prm.Data.Repositories;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using UserManagement.Configuration;
using UserManagement.Services;
using UserManagement.Services.Interfaces;

namespace Prm.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IEmployeeSkillRepository, EmployeeSkillRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
        return services;
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;

        services.AddAutoMapper(typeof(Prm.Data.SeedData).Assembly);

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

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordHasher<SystemConfiguration>, PasswordHasher<SystemConfiguration>>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMilestoneService, MilestoneService>();
        services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
        return services;
    }
}
