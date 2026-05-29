using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Data;

public static class SeedData
{
    public static readonly Role[] Roles =
    [
        new() { RoleId = (int)RoleNameEnum.Admin, Name = nameof(RoleNameEnum.Admin) },
        new() { RoleId = (int)RoleNameEnum.Manager, Name = nameof(RoleNameEnum.Manager) },
        new() { RoleId = (int)RoleNameEnum.Employee, Name = nameof(RoleNameEnum.Employee) },
    ];
}
