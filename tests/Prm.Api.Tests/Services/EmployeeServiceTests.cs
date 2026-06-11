using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task AssignManager_WhenEmployeeUserNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetByIdWithRole(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: 99, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.UserNotFound, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenEmployeeUserInactive_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateUser(isActive: false);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.UserInactive, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenDepartmentOrDesignationMissing_ThrowsArgumentException()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(
                employeeUserId: user.Id,
                managerUserId: 2,
                department: " ",
                designation: "Dev")));

        Assert.Equal(AppConstants.Employees.DepartmentAndDesignationRequired, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenInvalidEmployeeRole_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateUser(roleId: (int)RoleNameEnum.Manager);
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.InvalidRoleForManagerAssignment, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenInvalidManagerUser_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetByIdWithRole(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(x => x.GetByIdWithRole(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.InvalidManagerUser, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenSuccessful_SetsDepartmentDesignationAndManager()
    {
        var employeeUser = ApiTestData.CreateEmployeeUser();
        var managerUser = ApiTestData.CreateManager();

        _userRepository
            .Setup(x => x.GetByIdWithRole(employeeUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeUser);
        _userRepository
            .Setup(x => x.GetByIdWithRole(managerUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerUser);
        _userRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AssignManager(CreateAssignManagerRequest(
            employeeUserId: employeeUser.Id,
            managerUserId: managerUser.Id,
            department: "Backend",
            designation: "Senior Developer"));

        Assert.True(result);
        Assert.Equal("Backend", employeeUser.Department);
        Assert.Equal("Senior Developer", employeeUser.Designation);
        _userRepository.Verify(x => x.SetManager(employeeUser.Id, managerUser.Id, It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(x => x.Update(employeeUser), Times.Once);
    }

    [Fact]
    public async Task Update_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, new UpdateEmployeeRequest { Department = "Eng", Designation = "Lead" }));

        Assert.Equal(AppConstants.Employees.NotFound, exception.Message);
    }

    [Fact]
    public async Task Update_WhenUserInactive_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateEmployeeUser(userIsActive: false);
        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Update(user.Id, new UpdateEmployeeRequest { Department = "Eng", Designation = "Lead" }));

        Assert.Equal(AppConstants.Employees.AlreadyDeactivated, exception.Message);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsTrue()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.Update(
            user.Id,
            new UpdateEmployeeRequest { Department = "Product", Designation = "Senior Dev" });

        Assert.True(result);
        Assert.Equal("Product", user.Department);
        Assert.Equal("Senior Dev", user.Designation);
        _userRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyDeactivated_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateEmployeeUser(userIsActive: false);
        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Deactivate(user.Id));

        Assert.Equal(AppConstants.Employees.AlreadyDeactivated, exception.Message);
    }

    [Fact]
    public async Task Deactivate_WhenEmployeeRole_ClosesAllocationsAndSetsBench()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = ApiTestData.CreateEmployeeUser();
        user.Allocations.Add(new Allocation
        {
            Id = 1,
            UserId = user.Id,
            ProjectId = 1,
            UtilizationPercent = 50,
            FromDate = today.AddMonths(-1),
            ToDate = today.AddMonths(1),
        });

        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();
        var result = await sut.Deactivate(user.Id);

        Assert.True(result);
        Assert.False(user.IsActive);
        Assert.Equal(today, user.Allocations.Single().ToDate);
        _userRepository.Verify(
            x => x.SetCurrentResourceStatus(user.Id, (int)ResourceStatusTypeEnum.Bench, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEmployees_ReturnsAggregatedCounts()
    {
        var users = new List<User>
        {
            ApiTestData.CreateEmployeeUser(1, status: EmployeeConstants.StatusBench),
            ApiTestData.CreateEmployeeUser(2, status: EmployeeConstants.StatusAllocated),
            ApiTestData.CreateEmployeeUser(3, status: EmployeeConstants.StatusBench),
        };

        _userRepository
            .Setup(x => x.GetEmployeeUsers(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var sut = CreateSut();
        var result = await sut.GetEmployees(new EmployeeFilter());

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Allocated);
        Assert.Equal(2, result.Bench);
        Assert.Equal(3, result.Employees.Count);
    }

    [Fact]
    public async Task GetDetail_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetDetail(99));

        Assert.Equal(AppConstants.Manager.EmployeeNotFound, exception.Message);
    }

    [Fact]
    public async Task GetDetail_ReturnsEmployeeDetailWithSkillsAndAllocations()
    {
        var user = ApiTestData.CreateEmployeeUser();
        user.UserSkills =
        [
            new UserSkill
            {
                UserId = user.Id,
                SkillId = 1,
                Proficiency = "Expert",
                Skill = new Skill { Id = 1, Name = "C#", Category = "Language" },
            },
            new UserSkill
            {
                UserId = user.Id,
                SkillId = 2,
                Proficiency = "Intermediate",
                Skill = new Skill { Id = 2, Name = "Azure", Category = "Cloud" },
            },
        ];

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocation = ApiTestData.CreateAllocation(userId: user.Id, utilizationPercent: 50);
        var pastAllocation = ApiTestData.CreateAllocation(
            id: 2,
            userId: user.Id,
            utilizationPercent: 100,
            fromDate: today.AddMonths(-6),
            toDate: today.AddMonths(-1));

        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.Is<UserAllocationPeriodQuery>(query => query.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        _allocationRepository
            .Setup(x => x.GetActiveByUserId(user.Id, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation> { activeAllocation });
        _allocationRepository
            .Setup(x => x.GetPastByUserId(
                It.Is<UserPastAllocationsQuery>(query => query.UserId == user.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation> { pastAllocation });
        _timesheetRepository
            .Setup(x => x.GetRecentActivityTagNamesForUser(user.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Development" });

        var sut = CreateSut();
        var result = await sut.GetDetail(user.Id);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.FullName, result.Name);
        Assert.Equal(EmployeeConstants.StatusAllocated, result.CurrentStatus);
        Assert.Equal(50, result.UtilizationPercent);
        Assert.Equal("Azure, C#", result.ProfileSkills);
        Assert.Single(result.ActiveAllocations);
        Assert.Equal("Alpha", result.ActiveAllocations[0].Project);
        Assert.Single(result.PastAllocations);
        Assert.Equal(100, result.PastAllocations[0].UtilizationPercent);
        Assert.Single(result.RecentActivityTags);
        Assert.Equal("Development", result.RecentActivityTags[0]);
    }

    [Fact]
    public async Task GetUtilization_ReturnsBenchDescriptionWhenUtilizationIsZero()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();
        var result = await sut.GetUtilization(user.Id);

        Assert.Equal(user.Id, result.EmployeeUserId);
        Assert.Equal(0, result.UtilizationPercent);
        Assert.Equal(ManagerConstants.AvailabilityOnBench, result.StatusDescription);
    }

    [Fact]
    public async Task GetUtilization_ReturnsPercentWhenAllocated()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetEmployeeUserDetailById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        var sut = CreateSut();
        var result = await sut.GetUtilization(user.Id);

        Assert.Equal(75, result.UtilizationPercent);
        Assert.Equal("75%", result.StatusDescription);
    }

    private static AssignManagerRequest CreateAssignManagerRequest(
        int employeeUserId,
        int managerUserId,
        string department = "Engineering",
        string designation = "Developer") =>
        new()
        {
            EmployeeUserId = employeeUserId,
            ManagerUserId = managerUserId,
            Department = department,
            Designation = designation,
        };

    private EmployeeService CreateSut() =>
        new(
            _userRepository.Object,
            _allocationRepository.Object,
            _timesheetRepository.Object,
            _mapper);
}
