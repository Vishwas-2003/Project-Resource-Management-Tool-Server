using Prm.Common.Constants;
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

    public static readonly ActivityTag[] ActivityTags =
    [
        new() { Id = 1, Name = TimesheetConstants.StandardActivityTagNames[0], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 2, Name = TimesheetConstants.StandardActivityTagNames[1], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 3, Name = TimesheetConstants.StandardActivityTagNames[2], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 4, Name = TimesheetConstants.StandardActivityTagNames[3], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 5, Name = TimesheetConstants.StandardActivityTagNames[4], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 6, Name = TimesheetConstants.StandardActivityTagNames[5], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 7, Name = TimesheetConstants.StandardActivityTagNames[6], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 8, Name = TimesheetConstants.StandardActivityTagNames[7], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 9, Name = TimesheetConstants.StandardActivityTagNames[8], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 10, Name = TimesheetConstants.StandardActivityTagNames[9], CreatedAtUtc = SeedCreatedAtUtc },
        new() { Id = 11, Name = TimesheetConstants.StandardActivityTagNames[10], CreatedAtUtc = SeedCreatedAtUtc },
    ];
}
