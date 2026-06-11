using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Infrastructure;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Allocations;
using Prm.Common.Models.Manager;

namespace Prm.Api.Tests.Controllers;

public class AllocationControllerTests
{
    private readonly Mock<IAllocationService> _allocationService = new();
    private const int ManagerUserId = 10;

    [Fact]
    public async Task GetActive_WhenAllocationsExist_ReturnsOk()
    {
        var response = new ActiveAllocationsResponse
        {
            TotalActiveAllocations = 1,
            Allocations = [new ActiveAllocationRow { EmployeeName = "Jane Doe", ProjectName = "Alpha" }],
        };

        _allocationService
            .Setup(x => x.GetActiveAllocations(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetActive(null, CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<ActiveAllocationsResponse>(result);
        Assert.Equal(1, value.TotalActiveAllocations);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        var response = new AllocationCreatedResponse { AllocationId = 5, EmployeeName = "Jane Doe" };

        _allocationService
            .Setup(x => x.Create(It.IsAny<CreateAllocationRequest>(), ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.Create(
            new CreateAllocationRequest
            {
                EmployeeUserId = 1,
                ProjectId = 1,
                UtilizationPercent = 50,
                FromDate = new DateOnly(2026, 1, 1),
                ToDate = new DateOnly(2026, 6, 30),
            },
            CancellationToken.None);

        var value = ControllerTestHelper.AssertCreatedValue<AllocationCreatedResponse>(result);
        Assert.Equal(5, value.AllocationId);
    }

    [Fact]
    public async Task Create_WhenProjectNotOwned_Returns404()
    {
        _allocationService
            .Setup(x => x.Create(It.IsAny<CreateAllocationRequest>(), ManagerUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Manager.ProjectNotOwned));

        var sut = CreateSut();
        var result = await sut.Create(
            new CreateAllocationRequest { EmployeeUserId = 1, ProjectId = 1, UtilizationPercent = 50, FromDate = new DateOnly(2026, 1, 1) },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }

    [Fact]
    public async Task End_WhenValid_ReturnsOk()
    {
        var response = new AllocationEndedResponse { AllocationId = 1, EmployeeName = "Jane Doe" };

        _allocationService
            .Setup(x => x.End(1, ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.End(1, CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<AllocationEndedResponse>(result);
        Assert.Equal(1, value.AllocationId);
    }

    [Fact]
    public async Task GetByProject_WhenValid_ReturnsOk()
    {
        var response = new ProjectAllocationsResponse { ProjectId = 1, ProjectName = "Alpha" };

        _allocationService
            .Setup(x => x.GetByProjectId(1, ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetByProject(1, CancellationToken.None);

        var value = ControllerTestHelper.AssertOkValue<ProjectAllocationsResponse>(result);
        Assert.Equal("Alpha", value.ProjectName);
    }

    private AllocationController CreateSut() =>
        new(
            _allocationService.Object,
            ControllerTestHelper.CreateManagerAccess(
                ManagerUserId,
                ApiTestData.CreateUser(ManagerUserId, (int)RoleNameEnum.Manager)));
}
