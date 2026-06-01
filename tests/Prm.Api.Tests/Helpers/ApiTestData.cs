using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;

namespace Prm.Api.Tests.Helpers;

internal static class ApiTestData
{
    internal const string ValidPassword = "TempPass1!";

    internal static User CreateUser(
        int id = 1,
        int roleId = (int)RoleNameEnum.Employee,
        bool isActive = true,
        string username = "jdoe",
        Employee? employee = null)
    {
        var roleName = roleId switch
        {
            (int)RoleNameEnum.Admin => "Admin",
            (int)RoleNameEnum.Manager => "Manager",
            _ => "Employee",
        };

        return new User
        {
            Id = id,
            RoleId = roleId,
            FullName = "Jane Doe",
            Username = username,
            Email = $"{username}@prm.local",
            PasswordHash = string.Empty,
            IsActive = isActive,
            ForcePasswordChange = false,
            Role = new Role { Id = roleId, Name = roleName, CreatedAtUtc = DateTime.UtcNow },
            Employee = employee,
        };
    }

    internal static Employee CreateEmployee(
        int id = 1,
        int userId = 1,
        string? status = EmployeeConstants.StatusBench,
        bool userIsActive = true,
        int roleId = (int)RoleNameEnum.Employee)
    {
        var user = CreateUser(userId, roleId, userIsActive);
        return new Employee
        {
            Id = id,
            UserId = userId,
            Department = "Engineering",
            Designation = "Developer",
            Status = status,
            User = user,
            Allocations = new List<Allocation>(),
        };
    }

    internal static Employee CreateManager(int id = 10, int userId = 10) =>
        CreateEmployee(id, userId, status: null, roleId: (int)RoleNameEnum.Manager);

    internal static Project CreateProject(
        int id = 1,
        string name = "Alpha",
        DateOnly? start = null,
        DateOnly? end = null)
    {
        var startDate = start ?? new DateOnly(2026, 1, 1);
        var endDate = end ?? new DateOnly(2026, 12, 31);

        return new Project
        {
            Id = id,
            Name = name,
            Description = "Test project",
            StartDate = startDate,
            EndDate = endDate,
            Status = ProjectConstants.StatusPlanned,
            ManagerEmployeeId = 10,
            ManagerEmployee = CreateManager(),
        };
    }

    internal static Milestone CreateMilestone(int id = 1, int projectId = 1, string title = "Phase 1") =>
        new()
        {
            Id = id,
            ProjectId = projectId,
            Title = title,
            DueDate = new DateOnly(2026, 6, 1),
            Status = MilestoneConstants.StatusNotStarted,
        };

    internal static SystemConfiguration CreateConfiguration(
        int id,
        string value = "test-value",
        string configurationType = "Test") =>
        new()
        {
            Id = id,
            ConfigurationType = configurationType,
            Value = value,
        };
}
