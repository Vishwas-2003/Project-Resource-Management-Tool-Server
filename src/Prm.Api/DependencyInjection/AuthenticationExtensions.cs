using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prm.Api.Infrastructure;
using Prm.Common.Constants;
using UserManagement.Configuration;

namespace Prm.Api.DependencyInjection;

public static class AuthenticationExtensions
{
    public static IServiceCollection RegisterJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(AppConstants.Configuration.JwtSection).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
        {
            throw new InvalidOperationException(AppConstants.Messages.JwtSecretMissing);
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero,
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            await SessionExpiredResponseWriter.WriteAsync(context.Response);
                        }
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }
}
