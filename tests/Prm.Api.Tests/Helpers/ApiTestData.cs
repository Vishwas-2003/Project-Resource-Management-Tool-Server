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
        string department = "Engineering",
        string designation = "Developer",
        string? resourceStatus = null,
        int? managerUserId = null,
        string? fullName = null)
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
            FullName = fullName ?? "Jane Doe",
            Username = username,
            Email = $"{username}@prm.local",
            PasswordHash = string.Empty,
            IsActive = isActive,
            Department = department,
            Designation = designation,
            PasswordExpiryTime = null,
            Role = new Role { Id = roleId, Name = roleName, CreatedAtUtc = DateTime.UtcNow },
            ResourceStatusHistories = resourceStatus is not null
                ? [CreateResourceStatusHistory(id, resourceStatus)]
                : [],
            ManagerHistories = managerUserId is not null
                ? [CreateManagerHistory(id, managerUserId.Value)]
                : [],
            Allocations = [],
            UserSkills = [],
        };
    }

    internal static User CreateEmployeeUser(
        int id = 1,
        string? status = EmployeeConstants.StatusBench,
        bool userIsActive = true,
        int roleId = (int)RoleNameEnum.Employee,
        string? fullName = null,
        int? managerUserId = null) =>
        CreateUser(
            id,
            roleId,
            userIsActive,
            username: id == 1 ? "jdoe" : $"user{id}",
            department: "Engineering",
            designation: "Developer",
            resourceStatus: status,
            managerUserId: managerUserId,
            fullName: fullName);

    internal static User CreateManager(int id = 10) =>
        CreateUser(id: id, roleId: (int)RoleNameEnum.Manager, username: "manager");

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
            HealthStatus = ManagerConstants.HealthOnTrack,
            ManagerUserId = 10,
            ManagerUser = CreateManager(),
        };
    }

    internal static Milestone CreateMilestone(
        int id = 1,
        int projectId = 1,
        string title = "Phase 1",
        DateOnly? dueDate = null,
        string? status = null) =>
        new()
        {
            Id = id,
            ProjectId = projectId,
            Title = title,
            DueDate = dueDate ?? new DateOnly(2026, 6, 1),
            Status = status ?? MilestoneConstants.StatusNotStarted,
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

    internal static Allocation CreateAllocation(
        int id = 1,
        int userId = 1,
        int projectId = 1,
        int utilizationPercent = 50,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string employeeName = "Jane Doe",
        string projectName = "Alpha",
        int managerUserId = 10,
        User? user = null,
        Project? project = null)
    {
        user ??= CreateEmployeeUser(userId, fullName: employeeName);
        project ??= CreateProject(projectId, projectName);
        project.ManagerUserId = managerUserId;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new Allocation
        {
            Id = id,
            UserId = userId,
            ProjectId = projectId,
            UtilizationPercent = utilizationPercent,
            FromDate = fromDate ?? today.AddMonths(-1),
            ToDate = toDate ?? today.AddMonths(1),
            User = user,
            Project = project,
        };
    }

    internal static ActivityTag CreateActivityTag(int id = 1, string? name = null) =>
        new()
        {
            Id = id,
            Name = name ?? TimesheetConstants.StandardActivityTagNames[0],
        };

    internal static IReadOnlyList<ActivityTag> CreateStandardActivityTags() =>
        TimesheetConstants.StandardActivityTagNames
            .Select((name, index) => CreateActivityTag(index + 1, name))
            .ToList();

    private static ResourceStatusHistory CreateResourceStatusHistory(int userId, string status)
    {
        var statusTypeId = status == EmployeeConstants.StatusAllocated
            ? (int)ResourceStatusTypeEnum.Allocated
            : (int)ResourceStatusTypeEnum.Bench;

        return new ResourceStatusHistory
        {
            UserId = userId,
            ResourceStatusTypeId = statusTypeId,
            ResourceStatusType = new ResourceStatusType { Id = statusTypeId, Name = status },
            EffectiveFromUtc = DateTime.UtcNow,
            EffectiveToUtc = null,
        };
    }

    private static ResourceManagerHistory CreateManagerHistory(int userId, int managerUserId) =>
        new()
        {
            UserId = userId,
            ManagerUserId = managerUserId,
            EffectiveFromUtc = DateTime.UtcNow,
            EffectiveToUtc = null,
        };
}
