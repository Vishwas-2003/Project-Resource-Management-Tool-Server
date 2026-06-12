using Moq;
using Prm.Api.Services;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Tests.Services;

public class AllocationServiceTests
{
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    private static DateOnly From => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
    private static DateOnly To => From.AddMonths(3);
    private const int ManagerUserId = 10;

    [Fact]
    public async Task GetActiveAllocations_WhenNoFilter_ReturnsAll()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, resourceName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, userId: 2, projectId: 2, resourceName: "John Smith", projectName: "Beta"),
        };

        _allocationRepository
            .Setup(x => x.GetActiveAllocations(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetActiveAllocations(null);

        Assert.Equal(2, result.TotalActiveAllocations);
        Assert.Equal(2, result.Allocations.Count);
    }

    [Fact]
    public async Task GetActiveAllocations_WhenFilterMatchesResourceName_ReturnsEmployeeMatches()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, resourceName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, userId: 2, projectId: 2, resourceName: "John Smith", projectName: "Beta"),
        };

        _allocationRepository
            .Setup(x => x.GetActiveAllocations(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetActiveAllocations("jane");

        Assert.Single(result.Allocations);
        Assert.Equal("Jane Doe", result.Allocations[0].ResourceName);
    }

    [Fact]
    public async Task GetActiveAllocations_WhenNoEmployeeMatch_FiltersByProjectName()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, resourceName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, userId: 2, projectId: 2, resourceName: "John Smith", projectName: "Beta"),
        };

        _allocationRepository
            .Setup(x => x.GetActiveAllocations(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetActiveAllocations("beta");

        Assert.Single(result.Allocations);
        Assert.Equal("Beta", result.Allocations[0].ProjectName);
    }

    [Fact]
    public async Task GetActiveAllocations_WhenInvalidFilter_ThrowsArgumentException()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, resourceName: "Jane Doe", projectName: "Alpha"),
        };

        _allocationRepository
            .Setup(x => x.GetActiveAllocations(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetActiveAllocations("unknown"));

        Assert.Equal(AppConstants.Allocations.InvalidFilter, exception.Message);
    }

    [Fact]
    public async Task Create_WhenPastFromDate_ThrowsArgumentException()
    {
        SetupValidProject();
        SetupValidEmployee();

        var sut = CreateSut();
        var pastFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);
        var futureTo = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Create(CreateValidRequest(from: pastFrom, to: futureTo), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.PastDateNotAllowed, exception.Message);
    }

    [Fact]
    public async Task Create_WhenInvalidDateRange_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Create(CreateValidRequest(from: To, to: From), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.InvalidDateRange, exception.Message);
    }

    [Fact]
    public async Task Create_WhenInvalidUtilization_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Create(CreateValidRequest(utilizationPercent: 0), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.InvalidUtilization, exception.Message);
    }

    [Fact]
    public async Task Create_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        _projectRepository
            .Setup(x => x.GetByIdWithManager(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Projects.NotFound, exception.Message);
    }

    [Fact]
    public async Task Create_WhenProjectNotOwned_ThrowsUnauthorizedAccessException()
    {
        var project = ApiTestData.CreateProject();
        project.ManagerUserId = 99;
        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProjectNotOwned, exception.Message);
    }

    [Fact]
    public async Task Create_WhenProjectNotAllocatable_ThrowsInvalidOperationException()
    {
        SetupValidProject(status: ProjectConstants.StatusOnHold);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.ProjectNotAllocatable, exception.Message);
    }

    [Fact]
    public async Task Create_WhenResourceNotEligible_ThrowsKeyNotFoundException()
    {
        SetupValidProject();
        _userRepository
            .Setup(x => x.GetResourceUserDetailById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Manager.ResourceNotEligible, exception.Message);
    }

    [Fact]
    public async Task Create_WhenResourceNotUnderManager_ThrowsInvalidOperationException()
    {
        SetupValidProject();
        SetupValidEmployee();
        SetupResourceNotUnderManager();

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Manager.ResourceNotUnderManager, exception.Message);
    }

    [Fact]
    public async Task Create_WhenDatesBeforeEmployeeCreated_ThrowsArgumentException()
    {
        var project = ApiTestData.CreateProject(
            start: DateOnly.FromDateTime(DateTime.UtcNow),
            end: DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1));
        var employee = ApiTestData.CreateResourceUser();
        employee.CreatedAtUtc = DateTime.UtcNow.AddDays(14);

        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _userRepository
            .Setup(x => x.GetResourceUserDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupResourceUnderManager(employee.Id, ManagerUserId);

        var sut = CreateSut();
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var toDate = fromDate.AddMonths(1);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Create(
                CreateValidRequest(
                    projectId: project.Id,
                    resourceUserId: employee.Id,
                    from: fromDate,
                    to: toDate),
                ManagerUserId));

        Assert.Equal(AppConstants.Allocations.AllocationDatesBeforeResourceCreated, exception.Message);
    }

    [Fact]
    public async Task Create_WhenOverlappingAllocation_ThrowsInvalidOperationException()
    {
        SetupValidProject();
        SetupValidEmployee();
        _allocationRepository
            .Setup(x => x.HasOverlappingAllocationOnProject(
                It.IsAny<ProjectAllocationOverlapQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.OverlappingAllocationOnProject, exception.Message);
    }

    [Fact]
    public async Task Create_WhenExceedsMaxUtilization_ThrowsInvalidOperationException()
    {
        SetupValidProject();
        SetupValidEmployee();
        SetupNoOverlap();
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(60);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Create(CreateValidRequest(utilizationPercent: 50), ManagerUserId));

        Assert.Equal(AppConstants.Allocations.ExceedsMaxUtilization, exception.Message);
    }

    [Fact]
    public async Task Create_WhenSuccessful_UpdatesEmployeeStatus()
    {
        var project = ApiTestData.CreateProject();
        var employee = ApiTestData.CreateResourceUser(status: ResourceConstants.StatusBench);
        Allocation? savedAllocation = null;

        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _userRepository
            .Setup(x => x.GetResourceUserDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupResourceUnderManager(employee.Id, ManagerUserId);
        SetupNoOverlap();
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns((UserAllocationPeriodQuery query, CancellationToken _) =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var utilization = query.FromDate == today && query.ToDate == today ? 50 : 0;
                return Task.FromResult(utilization);
            });
        _allocationRepository
            .Setup(x => x.Add(It.IsAny<Allocation>(), It.IsAny<CancellationToken>()))
            .Callback<Allocation, CancellationToken>((allocation, _) =>
            {
                allocation.Id = 7;
                savedAllocation = allocation;
            })
            .Returns(Task.CompletedTask);
        _allocationRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _userRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Create(
            CreateValidRequest(projectId: project.Id, resourceUserId: employee.Id),
            ManagerUserId);

        Assert.Equal(7, result.AllocationId);
        Assert.Equal(employee.FullName, result.ResourceName);
        Assert.Equal(project.Name, result.ProjectName);
        Assert.NotNull(savedAllocation);
        Assert.Equal(50, savedAllocation!.UtilizationPercent);
        _userRepository.Verify(
            x => x.SetCurrentResourceStatus(
                employee.Id,
                (int)ResourceStatusTypeEnum.Allocated,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task End_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _allocationRepository
            .Setup(x => x.GetByIdWithDetails(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Allocation?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.End(1, ManagerUserId));

        Assert.Equal(AppConstants.Allocations.NotFound, exception.Message);
    }

    [Fact]
    public async Task End_WhenNotOwned_ThrowsUnauthorizedAccessException()
    {
        var allocation = ApiTestData.CreateAllocation(managerUserId: 99);
        _allocationRepository
            .Setup(x => x.GetByIdWithDetails(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.End(allocation.Id, ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProjectNotOwned, exception.Message);
    }

    [Fact]
    public async Task End_WhenAlreadyEnded_ThrowsInvalidOperationException()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var allocation = ApiTestData.CreateAllocation(toDate: today.AddDays(-1));
        _allocationRepository
            .Setup(x => x.GetByIdWithDetails(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.End(allocation.Id, ManagerUserId));

        Assert.Equal(AppConstants.Allocations.AlreadyEnded, exception.Message);
    }

    [Fact]
    public async Task End_WhenSuccessful_SetsEndDateToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var employee = ApiTestData.CreateResourceUser();
        var allocation = ApiTestData.CreateAllocation(toDate: today.AddMonths(1));
        allocation.User = employee;

        _allocationRepository
            .Setup(x => x.GetByIdWithDetails(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _allocationRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _userRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.End(allocation.Id, ManagerUserId);

        Assert.Equal(today, allocation.ToDate);
        Assert.Equal(today, result.EndDate);
        Assert.Equal(allocation.Id, result.AllocationId);
        _allocationRepository.Verify(x => x.Update(allocation), Times.Once);
        _allocationRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByProjectId_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        _projectRepository
            .Setup(x => x.GetByIdWithManager(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetByProjectId(1, ManagerUserId));

        Assert.Equal(AppConstants.Projects.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetByProjectId_WhenProjectNotOwned_ThrowsUnauthorizedAccessException()
    {
        var project = ApiTestData.CreateProject();
        project.ManagerUserId = 99;
        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.GetByProjectId(project.Id, ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProjectNotOwned, exception.Message);
    }

    [Fact]
    public async Task GetByProjectId_WhenSuccessful_ReturnsProjectAllocations()
    {
        var project = ApiTestData.CreateProject();
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, projectId: project.Id, projectName: project.Name),
            ApiTestData.CreateAllocation(2, userId: 2, projectId: project.Id, projectName: project.Name),
        };

        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _allocationRepository
            .Setup(x => x.GetActiveByProjectId(project.Id, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetByProjectId(project.Id, ManagerUserId);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(project.Name, result.ProjectName);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Equal(1, result.Allocations[0].RowNumber);
        Assert.Equal(2, result.Allocations[1].RowNumber);
    }

    private void SetupValidProject(string status = ProjectConstants.StatusPlanned)
    {
        var project = ApiTestData.CreateProject();
        project.Status = status;
        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
    }

    private void SetupValidEmployee()
    {
        var employee = ApiTestData.CreateResourceUser(roleId: (int)RoleNameEnum.Employee);
        _userRepository
            .Setup(x => x.GetResourceUserDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupResourceUnderManager(employee.Id, ManagerUserId);
    }

    private void SetupResourceUnderManager(int resourceUserId, int managerUserId)
    {
        _userRepository
            .Setup(x => x.IsResourceManagedByManager(
                resourceUserId,
                managerUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupResourceNotUnderManager(int resourceUserId = 1, int managerUserId = ManagerUserId)
    {
        _userRepository
            .Setup(x => x.IsResourceManagedByManager(
                resourceUserId,
                managerUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupNoOverlap()
    {
        _allocationRepository
            .Setup(x => x.HasOverlappingAllocationOnProject(
                It.IsAny<ProjectAllocationOverlapQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private static CreateAllocationRequest CreateValidRequest(
        int projectId = 1,
        int resourceUserId = 1,
        int utilizationPercent = 50,
        DateOnly? from = null,
        DateOnly? to = null) =>
        new()
        {
            ProjectId = projectId,
            ResourceUserId = resourceUserId,
            UtilizationPercent = utilizationPercent,
            FromDate = from ?? From,
            ToDate = to ?? To,
        };

    private AllocationService CreateSut() =>
        new(
            _allocationRepository.Object,
            _userRepository.Object,
            _projectRepository.Object);
}
