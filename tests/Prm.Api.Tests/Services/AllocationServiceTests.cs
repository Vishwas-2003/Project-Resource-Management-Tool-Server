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
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();

    private static readonly DateOnly From = new(2026, 3, 1);
    private static readonly DateOnly To = new(2026, 6, 30);
    private const int ManagerUserId = 10;

    [Fact]
    public async Task GetActiveAllocations_WhenNoFilter_ReturnsAll()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, employeeName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, employeeId: 2, projectId: 2, employeeName: "John Smith", projectName: "Beta"),
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
    public async Task GetActiveAllocations_WhenFilterMatchesEmployeeName_ReturnsEmployeeMatches()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, employeeName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, employeeId: 2, projectId: 2, employeeName: "John Smith", projectName: "Beta"),
        };

        _allocationRepository
            .Setup(x => x.GetActiveAllocations(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        var sut = CreateSut();
        var result = await sut.GetActiveAllocations("jane");

        Assert.Single(result.Allocations);
        Assert.Equal("Jane Doe", result.Allocations[0].EmployeeName);
    }

    [Fact]
    public async Task GetActiveAllocations_WhenNoEmployeeMatch_FiltersByProjectName()
    {
        var allocations = new List<Allocation>
        {
            ApiTestData.CreateAllocation(1, employeeName: "Jane Doe", projectName: "Alpha"),
            ApiTestData.CreateAllocation(2, employeeId: 2, projectId: 2, employeeName: "John Smith", projectName: "Beta"),
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
            ApiTestData.CreateAllocation(1, employeeName: "Jane Doe", projectName: "Alpha"),
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
    public async Task Create_WhenEmployeeNotEligible_ThrowsKeyNotFoundException()
    {
        SetupValidProject();
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Create(CreateValidRequest(), ManagerUserId));

        Assert.Equal(AppConstants.Manager.EmployeeNotEligible, exception.Message);
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
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.IsAny<EmployeeAllocationPeriodQuery>(),
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
        var employee = ApiTestData.CreateEmployee(status: EmployeeConstants.StatusBench);
        Allocation? savedAllocation = null;

        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        SetupNoOverlap();
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.IsAny<EmployeeAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .Returns((EmployeeAllocationPeriodQuery query, CancellationToken _) =>
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
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _employeeRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Create(
            CreateValidRequest(projectId: project.Id, employeeId: employee.Id),
            ManagerUserId);

        Assert.Equal(7, result.AllocationId);
        Assert.Equal(employee.User.FullName, result.EmployeeName);
        Assert.Equal(project.Name, result.ProjectName);
        Assert.NotNull(savedAllocation);
        Assert.Equal(50, savedAllocation!.UtilizationPercent);
        Assert.Equal(EmployeeConstants.StatusAllocated, employee.Status);
        _employeeRepository.Verify(x => x.Update(employee), Times.Once);
        _employeeRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
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
        var employee = ApiTestData.CreateEmployee();
        var allocation = ApiTestData.CreateAllocation(toDate: today.AddMonths(1));
        allocation.Employee = employee;

        _allocationRepository
            .Setup(x => x.GetByIdWithDetails(allocation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _allocationRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _allocationRepository
            .Setup(x => x.SumUtilizationForEmployeeInPeriod(
                It.IsAny<EmployeeAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _employeeRepository
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
            ApiTestData.CreateAllocation(2, employeeId: 2, projectId: project.Id, projectName: project.Name),
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
        var employee = ApiTestData.CreateEmployee(roleId: (int)RoleNameEnum.Employee);
        _employeeRepository
            .Setup(x => x.GetEmployeeDetailById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
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
        int employeeId = 1,
        int utilizationPercent = 50,
        DateOnly? from = null,
        DateOnly? to = null) =>
        new()
        {
            ProjectId = projectId,
            EmployeeId = employeeId,
            UtilizationPercent = utilizationPercent,
            FromDate = from ?? From,
            ToDate = to ?? To,
        };

    private AllocationService CreateSut() =>
        new(
            _allocationRepository.Object,
            _employeeRepository.Object,
            _projectRepository.Object);
}
