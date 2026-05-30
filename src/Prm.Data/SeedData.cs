using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Data;

public static class SeedData
{
    private static readonly DateTime SeedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Role[] Roles =
    [
        new() { RoleId = (int)RoleNameEnum.Admin, Name = nameof(RoleNameEnum.Admin), CreatedAtUtc = SeedCreatedAtUtc },
        new() { RoleId = (int)RoleNameEnum.Manager, Name = nameof(RoleNameEnum.Manager), CreatedAtUtc = SeedCreatedAtUtc },
        new() { RoleId = (int)RoleNameEnum.Employee, Name = nameof(RoleNameEnum.Employee), CreatedAtUtc = SeedCreatedAtUtc },
    ];
}
