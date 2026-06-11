using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class ProjectHealthServiceTests
{
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly Mock<ISystemConfigurationRepository> _systemConfigurationRepository = new();

    [Fact]
    public async Task Evaluate_WhenNoIssues_ReturnsOnTrack()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = BuildProject(
            milestones:
            [
                ApiTestData.CreateMilestone(dueDate: today.AddDays(30)),
            ],
            allocations:
            [
                ApiTestData.CreateAllocation(utilizationPercent: 100),
            ]);

        SetupMaxWeeklyHours(40);
        SetupHoursWorked(1, project.Id, 40);

        var sut = CreateSut();
        var result = await sut.Evaluate(project);

        Assert.Equal(ManagerConstants.HealthOnTrack, result.HealthStatus);
        Assert.Contains(
            result.RiskFlags,
            flag => flag.Outcome == ManagerConstants.RiskFlagPass
                && flag.Message == ManagerConstants.ResourcesCorrectlyAllocated);
    }

    [Fact]
    public async Task Evaluate_WhenTwoOrMoreFailFlags_ReturnsAtRisk()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = BuildProject(
            milestones:
            [
                ApiTestData.CreateMilestone(
                    title: "Phase 1",
                    dueDate: today.AddDays(-5),
                    status: MilestoneConstants.StatusInProgress),
            ],
            allocations:
            [
                ApiTestData.CreateAllocation(utilizationPercent: 100),
            ]);

        SetupMaxWeeklyHours(40);
        SetupHoursWorked(1, project.Id, 10);

        var sut = CreateSut();
        var result = await sut.Evaluate(project);

        Assert.Equal(ManagerConstants.HealthAtRisk, result.HealthStatus);
        Assert.Equal(2, result.RiskFlags.Count(flag => flag.Outcome == ManagerConstants.RiskFlagFail));
    }

    [Fact]
    public async Task Evaluate_WhenOneFailFlag_ReturnsAttention()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = BuildProject(
            milestones:
            [
                ApiTestData.CreateMilestone(dueDate: today.AddDays(30)),
            ],
            allocations:
            [
                ApiTestData.CreateAllocation(utilizationPercent: 30),
            ]);

        SetupMaxWeeklyHours(40);
        SetupHoursWorked(1, project.Id, 12);

        var sut = CreateSut();
        var result = await sut.Evaluate(project);

        Assert.Equal(ManagerConstants.HealthAttention, result.HealthStatus);
        Assert.Single(result.RiskFlags, flag => flag.Outcome == ManagerConstants.RiskFlagFail);
    }

    [Fact]
    public async Task Evaluate_WhenMaxWeeklyHoursConfigMissing_UsesDefaultMaxWeeklyHours()
    {
        var project = BuildProject(
            allocations:
            [
                ApiTestData.CreateAllocation(utilizationPercent: 50),
            ]);

        _systemConfigurationRepository
            .Setup(x => x.GetById((int)ConfigurationOptionEnum.MaxWeeklyHours, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);

        var expectedHours = ManagerConstants.DefaultMaxWeeklyHours / 2;
        SetupHoursWorked(1, project.Id, expectedHours - 1);

        var sut = CreateSut();
        var result = await sut.Evaluate(project);

        var lowHoursFlag = result.RiskFlags.FirstOrDefault(flag =>
            flag.Message.Contains($"expected {expectedHours} hrs", StringComparison.Ordinal));
        Assert.NotNull(lowHoursFlag);
        Assert.Equal(ManagerConstants.RiskFlagFail, lowHoursFlag!.Outcome);
    }

    private ProjectHealthService CreateSut() =>
        new(_timesheetRepository.Object, _systemConfigurationRepository.Object);

    private static Project BuildProject(
        IEnumerable<Milestone>? milestones = null,
        IEnumerable<Allocation>? allocations = null)
    {
        var project = ApiTestData.CreateProject();
        project.Milestones = milestones?.ToList() ?? [];
        project.Allocations = allocations?.ToList() ?? [];
        return project;
    }

    private void SetupMaxWeeklyHours(int hours)
    {
        _systemConfigurationRepository
            .Setup(x => x.GetById((int)ConfigurationOptionEnum.MaxWeeklyHours, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateConfiguration((int)ConfigurationOptionEnum.MaxWeeklyHours, hours.ToString()));
    }

    private void SetupHoursWorked(int employeeUserId, int projectId, int hours)
    {
        _timesheetRepository
            .Setup(x => x.GetHoursWorkedForUserOnProjectInWeek(
                employeeUserId,
                projectId,
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hours);
    }
}
