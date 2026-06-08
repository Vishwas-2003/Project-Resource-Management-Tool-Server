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
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task AssignManager_WhenEmployeeUserNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(99, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetByIdWithRoleAndEmployee(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.UserInactive, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenDepartmentOrDesignationMissing_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateEmployee();
        var user = ApiTestData.CreateUser(employee: employee);
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(user.Id, It.IsAny<CancellationToken>()))
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
            .Setup(x => x.GetByIdWithRoleAndEmployee(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.InvalidRoleForManagerAssignment, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenInvalidManagerUser_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateEmployee();
        var user = ApiTestData.CreateUser(employee: employee);
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AssignManager(CreateAssignManagerRequest(employeeUserId: user.Id, managerUserId: 2)));

        Assert.Equal(AppConstants.Employees.InvalidManagerUser, exception.Message);
    }

    [Fact]
    public async Task AssignManager_WhenProfileMissing_CreatesEmployeeProfile()
    {
        var employeeUser = ApiTestData.CreateUser(roleId: (int)RoleNameEnum.Employee);
        var managerUser = ApiTestData.CreateUser(
            id: 5,
            roleId: (int)RoleNameEnum.Manager);
        Employee? saved = null;

        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(employeeUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeUser);
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(managerUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerUser);
        _employeeRepository
            .Setup(x => x.Add(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Callback<Employee, CancellationToken>((employee, _) => saved = employee)
            .Returns(Task.CompletedTask);
        _employeeRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AssignManager(CreateAssignManagerRequest(
            employeeUserId: employeeUser.Id,
            managerUserId: managerUser.Id,
            department: "Frontend",
            designation: "UI Developer"));

        Assert.True(result);
        Assert.NotNull(saved);
        Assert.Equal(employeeUser.Id, saved!.UserId);
        Assert.Equal(managerUser.Id, saved.ManagerUserId);
        Assert.Equal("Frontend", saved.Department);
        Assert.Equal("UI Developer", saved.Designation);
        Assert.Equal(EmployeeConstants.StatusBench, saved.Status);
        _employeeRepository.Verify(x => x.Update(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task AssignManager_WhenSuccessful_SetsManagerUserIdAndProfile()
    {
        var employee = ApiTestData.CreateEmployee();
        var employeeUser = ApiTestData.CreateUser(employee: employee);
        var managerUser = ApiTestData.CreateUser(
            id: 5,
            roleId: (int)RoleNameEnum.Manager);

        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(employeeUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeUser);
        _userRepository
            .Setup(x => x.GetByIdWithRoleAndEmployee(managerUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerUser);
        _employeeRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AssignManager(CreateAssignManagerRequest(
            employeeUserId: employeeUser.Id,
            managerUserId: managerUser.Id,
            department: "Backend",
            designation: "Senior Developer"));

        Assert.True(result);
        Assert.Equal(managerUser.Id, employee.ManagerUserId);
        Assert.Equal("Backend", employee.Department);
        Assert.Equal("Senior Developer", employee.Designation);
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

    [Fact]
    public async Task Update_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _employeeRepository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, new UpdateEmployeeRequest { Department = "Eng", Designation = "Lead" }));

        Assert.Equal(AppConstants.Employees.NotFound, exception.Message);
    }

    [Fact]
    public async Task Update_WhenUserInactive_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateEmployee(userIsActive: false);
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Update(employee.Id, new UpdateEmployeeRequest { Department = "Eng", Designation = "Lead" }));

        Assert.Equal(AppConstants.Employees.AlreadyDeactivated, exception.Message);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsTrue()
    {
        var employee = ApiTestData.CreateEmployee();
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();
        var result = await sut.Update(
            employee.Id,
            new UpdateEmployeeRequest { Department = "Product", Designation = "Senior Dev" });

        Assert.True(result);
        Assert.Equal("Product", employee.Department);
        Assert.Equal("Senior Dev", employee.Designation);
        _employeeRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyDeactivated_ThrowsInvalidOperationException()
    {
        var employee = ApiTestData.CreateEmployee(userIsActive: false);
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Deactivate(employee.Id));

        Assert.Equal(AppConstants.Employees.AlreadyDeactivated, exception.Message);
    }

    [Fact]
    public async Task Deactivate_WhenEmployeeRole_ClosesAllocationsAndSetsBench()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employee = ApiTestData.CreateEmployee(roleId: (int)RoleNameEnum.Employee);
        employee.Allocations.Add(new Allocation
        {
            Id = 1,
            EmployeeId = employee.Id,
            ProjectId = 1,
            UtilizationPercent = 50,
            FromDate = today.AddMonths(-1),
            ToDate = today.AddMonths(1),
        });

        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();
        var result = await sut.Deactivate(employee.Id);

        Assert.True(result);
        Assert.False(employee.User.IsActive);
        Assert.Equal(EmployeeConstants.StatusBench, employee.Status);
        Assert.Equal(today, employee.Allocations.Single().ToDate);
    }

    [Fact]
    public async Task GetEmployees_ReturnsAggregatedCounts()
    {
        var employees = new List<Employee>
        {
            ApiTestData.CreateEmployee(1, status: EmployeeConstants.StatusBench),
            ApiTestData.CreateEmployee(2, userId: 2, status: EmployeeConstants.StatusAllocated),
            ApiTestData.CreateEmployee(3, userId: 3, status: EmployeeConstants.StatusBench),
        };

        _employeeRepository
            .Setup(x => x.GetEmployees(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);

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
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetDetail(99));

        Assert.Equal(AppConstants.Manager.EmployeeNotFound, exception.Message);
    }

    [Fact]
    public async Task GetDetail_ReturnsEmployeeDetailWithSkillsAndAllocations()
    {
        var employee = ApiTestData.CreateEmployee();
        employee.EmployeeSkills =
        [
            new EmployeeSkill
            {
                EmployeeId = employee.Id,
                SkillId = 1,
                Proficiency = "Expert",
                Skill = new Skill { Id = 1, Name = "C#", Category = "Language" },
            },
            new EmployeeSkill
            {
                EmployeeId = employee.Id,
                SkillId = 2,
                Proficiency = "Intermediate",
                Skill = new Skill { Id = 2, Name = "Azure", Category = "Cloud" },
            },
        ];

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeAllocation = ApiTestData.CreateAllocation(employeeId: employee.Id, utilizationPercent: 50);
        var pastAllocation = ApiTestData.CreateAllocation(
            id: 2,
            employeeId: employee.Id,
            utilizationPercent: 100,
            fromDate: today.AddMonths(-6),
            toDate: today.AddMonths(-1));

        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.Is<EmployeeAllocationPeriodQuery>(query => query.EmployeeId == employee.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);
        _allocationRepository
            .Setup(x => x.GetActiveByEmployeeId(employee.Id, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation> { activeAllocation });
        _allocationRepository
            .Setup(x => x.GetPastByEmployeeId(
                It.Is<EmployeePastAllocationsQuery>(query => query.EmployeeId == employee.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation> { pastAllocation });
        _timesheetRepository
            .Setup(x => x.GetRecentActivityTagNamesForEmployee(employee.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Development" });

        var sut = CreateSut();
        var result = await sut.GetDetail(employee.Id);

        Assert.Equal(employee.Id, result.Id);
        Assert.Equal(employee.User.FullName, result.Name);
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
        var employee = ApiTestData.CreateEmployee();
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.IsAny<EmployeeAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();
        var result = await sut.GetUtilization(employee.Id);

        Assert.Equal(employee.Id, result.EmployeeId);
        Assert.Equal(0, result.UtilizationPercent);
        Assert.Equal(ManagerConstants.AvailabilityOnBench, result.StatusDescription);
    }

    [Fact]
    public async Task GetUtilization_ReturnsPercentWhenAllocated()
    {
        var employee = ApiTestData.CreateEmployee();
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.IsAny<EmployeeAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        var sut = CreateSut();
        var result = await sut.GetUtilization(employee.Id);

        Assert.Equal(75, result.UtilizationPercent);
        Assert.Equal("75%", result.StatusDescription);
    }

    private EmployeeService CreateSut() =>
        new(
            _employeeRepository.Object,
            _userRepository.Object,
            _allocationRepository.Object,
            _timesheetRepository.Object,
            _mapper);
}
