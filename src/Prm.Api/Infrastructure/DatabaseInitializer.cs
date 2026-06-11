using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Prm.Api.Configuration;
using Prm.Common.Enums;
using Prm.Data;
using Prm.Data.Entities;
using Prm.Data.Persistence;

namespace Prm.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task Initialize(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bootstrapAdmin = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.Users.AnyAsync(x => x.Username == bootstrapAdmin.Username, cancellationToken))
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var adminUser = new User
            {
                RoleId = (int)RoleNameEnum.Admin,
                FullName = bootstrapAdmin.FullName,
                Username = bootstrapAdmin.Username,
                Email = bootstrapAdmin.Email,
                PasswordHash = string.Empty,
                IsActive = true,
                Department = string.Empty,
                Designation = string.Empty,
                PasswordExpiryTime = DateTime.UtcNow,
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, bootstrapAdmin.Password);
            await dbContext.Users.AddAsync(adminUser, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
