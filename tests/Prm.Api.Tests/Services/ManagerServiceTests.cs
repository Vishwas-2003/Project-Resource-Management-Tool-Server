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
        var user = ApiTestData.CreateResourceUser(id: 1, status: ResourceConstants.StatusBench);
        _userRepository
            .Setup(x => x.GetResourcePoolUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        SetupUtilization(user.Id, 0);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Single(result.BenchResources);
        Assert.Empty(result.ActiveResources);
        Assert.Equal(user.Id, result.BenchResources[0].Id);
        Assert.Equal(user.FullName, result.BenchResources[0].Name);
        Assert.Equal(user.Department, result.BenchResources[0].Department);
        Assert.Equal(1, result.Summary.BenchCount);
        Assert.Equal(0, result.Summary.PartialCount);
    }

    [Fact]
    public async Task GetResourceDashboard_WhenPartialUtilization_ReturnsActiveEmployeeAndPartialCount()
    {
        SetupValidManager();
        var user = ApiTestData.CreateResourceUser(id: 2, status: ResourceConstants.StatusAllocated);
        _userRepository
            .Setup(x => x.GetResourcePoolUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        SetupUtilization(user.Id, 50);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Empty(result.BenchResources);
        Assert.Single(result.ActiveResources);
        Assert.Equal(50, result.ActiveResources[0].AllocationPercent);
        Assert.Equal("50% free", result.ActiveResources[0].Availability);
        Assert.Equal(0, result.Summary.BenchCount);
        Assert.Equal(1, result.Summary.PartialCount);
    }

    [Fact]
    public async Task GetResourceDashboard_AssignsRowNumbersSequentiallyAcrossBenchAndActive()
    {
        SetupValidManager();

        var benchUser = ApiTestData.CreateResourceUser(id: 1, status: ResourceConstants.StatusBench);
        var activeUser = ApiTestData.CreateResourceUser(id: 2, status: ResourceConstants.StatusAllocated);
        var fullUser = ApiTestData.CreateResourceUser(id: 3, status: ResourceConstants.StatusAllocated);

        _userRepository
            .Setup(x => x.GetResourcePoolUsers(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { benchUser, activeUser, fullUser });

        SetupUtilization(benchUser.Id, 0);
        SetupUtilization(activeUser.Id, 50);
        SetupUtilization(fullUser.Id, 100);

        var sut = CreateSut();
        var result = await sut.GetResourceDashboard(ManagerUserId);

        Assert.Equal(1, result.BenchResources[0].RowNumber);
        Assert.Equal(2, result.ActiveResources[0].RowNumber);
        Assert.Equal(3, result.ActiveResources[1].RowNumber);
        Assert.Equal(ManagerConstants.AvailabilityFull, result.ActiveResources[1].Availability);
        Assert.Equal(1, result.Summary.PartialCount);
    }

    private void SetupValidManager()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateManager());
    }

    private void SetupUtilization(int userId, int utilization)
    {
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.Is<UserAllocationPeriodQuery>(query => query.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utilization);
    }

    private ManagerService CreateSut() =>
        new(
            _userRepository.Object,
            _allocationRepository.Object);
}
