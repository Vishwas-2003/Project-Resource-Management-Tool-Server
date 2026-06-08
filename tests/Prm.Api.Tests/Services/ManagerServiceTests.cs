using Moq;
using Prm.Api.Services;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Tests.Services;

public class ManagerServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IAllocationRepository> _allocationRepository = new();

    private const int ManagerUserId = 10;

    [Fact]
    public async Task GetResourceDashboard_WhenManagerNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetResourceDashboard(ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProfileNotFound, exception.Message);
    }

    [Fact]
    public async Task GetResourceDashboard_WhenUtilizationZero_ReturnsBenchEmployee()
    {
        SetupValidManager();
        var employee = ApiTestData.CreateEmployee(id: 1, status: EmployeeConstants.StatusBench);
        _employeeRepository
            .Setup(x => x.GetResourcePoolEmployees(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { employee });
        SetupUtilization(employee.Id, 0);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Single(result.BenchEmployees);
        Assert.Empty(result.ActiveEmployees);
        Assert.Equal(employee.Id, result.BenchEmployees[0].Id);
        Assert.Equal(employee.User.FullName, result.BenchEmployees[0].Name);
        Assert.Equal(employee.Department, result.BenchEmployees[0].Department);
        Assert.Equal(1, result.Summary.BenchCount);
        Assert.Equal(0, result.Summary.PartialCount);
    }

    [Fact]
    public async Task GetResourceDashboard_WhenPartialUtilization_ReturnsActiveEmployeeAndPartialCount()
    {
        SetupValidManager();
        var employee = ApiTestData.CreateEmployee(id: 2, userId: 2, status: EmployeeConstants.StatusAllocated);
        _employeeRepository
            .Setup(x => x.GetResourcePoolEmployees(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { employee });
        SetupUtilization(employee.Id, 50);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Empty(result.BenchEmployees);
        Assert.Single(result.ActiveEmployees);
        Assert.Equal(50, result.ActiveEmployees[0].AllocationPercent);
        Assert.Equal("50% free", result.ActiveEmployees[0].Availability);
        Assert.Equal(0, result.Summary.BenchCount);
        Assert.Equal(1, result.Summary.PartialCount);
    }

    [Fact]
    public async Task GetResourceDashboard_AssignsRowNumbersSequentiallyAcrossBenchAndActive()
    {
        SetupValidManager();

        var benchEmployee = ApiTestData.CreateEmployee(id: 1, status: EmployeeConstants.StatusBench);
        var activeEmployee = ApiTestData.CreateEmployee(id: 2, userId: 2, status: EmployeeConstants.StatusAllocated);
        var fullEmployee = ApiTestData.CreateEmployee(id: 3, userId: 3, status: EmployeeConstants.StatusAllocated);

        _employeeRepository
            .Setup(x => x.GetResourcePoolEmployees(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { benchEmployee, activeEmployee, fullEmployee });

        SetupUtilization(benchEmployee.Id, 0);
        SetupUtilization(activeEmployee.Id, 50);
        SetupUtilization(fullEmployee.Id, 100);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Equal(1, result.BenchEmployees[0].RowNumber);
        Assert.Equal(2, result.ActiveEmployees[0].RowNumber);
        Assert.Equal(3, result.ActiveEmployees[1].RowNumber);
        Assert.Equal(ManagerConstants.AvailabilityFull, result.ActiveEmployees[1].Availability);
        Assert.Equal(1, result.Summary.PartialCount);
    }

    private void SetupValidManager()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateUser(id: ManagerUserId, roleId: (int)RoleNameEnum.Manager));
    }

    private void SetupUtilization(int employeeId, int utilization)
    {
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.Is<EmployeeAllocationPeriodQuery>(query => query.EmployeeId == employeeId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilization);
    }

    private ManagerService CreateSut() =>
        new(
            _userRepository.Object,
            _employeeRepository.Object,
            _allocationRepository.Object);
}
