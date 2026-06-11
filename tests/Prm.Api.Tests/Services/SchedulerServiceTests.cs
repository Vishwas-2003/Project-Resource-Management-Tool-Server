using Microsoft.Extensions.Logging;
using Moq;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Employees;
using Prm.Common.Models.Manager;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class SchedulerServiceTests
{
    private readonly Mock<IAllocationRepository> _allocationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IProjectRiskFlagRepository> _projectRiskFlagRepository = new();
    private readonly Mock<IProjectHealthService> _projectHealthService = new();
    private readonly Mock<ILogger<SchedulerService>> _logger = new();

    [Fact]
    public async Task Execute_WhenEmployeeHasNoUtilization_SetsStatusToBench()
    {
        var user = ApiTestData.CreateEmployeeUser(status: EmployeeConstants.StatusAllocated);
        _userRepository
            .Setup(x => x.GetEmployeeUsers(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        SetupEmptyProjectHealth();

        var sut = CreateSut();
        await sut.Execute();

        _userRepository.Verify(
            x => x.SetCurrentResourceStatus(user.Id, (int)ResourceStatusTypeEnum.Bench, It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_UpdatesProjectHealthAndReplacesRiskFlags()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetEmployeeUsers(It.IsAny<EmployeeFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _allocationRepository
            .Setup(x => x.SumUtilizationForUserInPeriod(
                It.IsAny<UserAllocationPeriodQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var projectSummary = ApiTestData.CreateProject();
        var projectDetails = ApiTestData.CreateProject();
        projectDetails.Milestones = [];
        projectDetails.Allocations = [];

        var evaluation = new ProjectHealthEvaluation
        {
            HealthStatus = ManagerConstants.HealthAtRisk,
            RiskFlags =
            [
                new RiskFlagItem
                {
                    Outcome = ManagerConstants.RiskFlagFail,
                    Message = "Test risk flag",
                },
            ],
        };

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { projectSummary });
        _projectRepository
            .Setup(x => x.GetByIdWithDetails(projectSummary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectDetails);
        _projectHealthService
            .Setup(x => x.Evaluate(projectDetails, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluation);

        var sut = CreateSut();
        await sut.Execute();

        Assert.Equal(ManagerConstants.HealthAtRisk, projectDetails.HealthStatus);
        _projectRepository.Verify(x => x.Update(projectDetails), Times.Once);
        _projectRiskFlagRepository.Verify(
            x => x.ReplaceForProject(
                projectDetails.Id,
                It.Is<IReadOnlyList<ProjectRiskFlag>>(flags =>
                    flags.Count == 1
                    && flags[0].Outcome == ManagerConstants.RiskFlagFail
                    && flags[0].Message == "Test risk flag"
                    && flags[0].SortOrder == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _projectRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupEmptyProjectHealth()
    {
        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Project>());
    }

    private SchedulerService CreateSut() =>
        new(
            _allocationRepository.Object,
            _userRepository.Object,
            _projectRepository.Object,
            _projectRiskFlagRepository.Object,
            _projectHealthService.Object,
            _logger.Object);
}
