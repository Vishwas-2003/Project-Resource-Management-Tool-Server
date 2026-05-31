using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Data;

public static class SeedData
{
    private static readonly DateTime SeedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Role[] Roles =
    [
        new() { Id = (int)RoleNameEnum.Admin, Name = nameof(RoleNameEnum.Admin), CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = (int)RoleNameEnum.Manager, Name = nameof(RoleNameEnum.Manager), CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = (int)RoleNameEnum.Employee, Name = nameof(RoleNameEnum.Employee), CreatedAtUtc = SeedCreatedAtUtc },
    ];

    public static readonly SystemConfiguration[] SystemConfigurations =
    [
        new() { Id = (int)ConfigurationOptionEnum.Provider, ConfigurationType = nameof(ConfigurationOptionEnum.Provider), Value = string.Empty, CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = (int)ConfigurationOptionEnum.ApiKey, ConfigurationType = nameof(ConfigurationOptionEnum.ApiKey), Value = string.Empty, CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = (int)ConfigurationOptionEnum.SchedulerInterval, ConfigurationType = nameof(ConfigurationOptionEnum.SchedulerInterval), Value = string.Empty, CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = (int)ConfigurationOptionEnum.MaxWeeklyHours, ConfigurationType = nameof(ConfigurationOptionEnum.MaxWeeklyHours), Value = string.Empty, CreatedAtUtc = SeedCreatedAtUtc },
    ];
}
