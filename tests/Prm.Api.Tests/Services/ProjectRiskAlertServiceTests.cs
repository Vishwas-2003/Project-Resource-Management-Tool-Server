using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Api.Models.Ai;
using Prm.Api.Models.Email;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class ProjectRiskAlertServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IProjectRiskFlagRepository> _projectRiskFlagRepository = new();
    private readonly Mock<IEmailNotificationHistoryRepository> _emailNotificationHistoryRepository = new();
    private readonly Mock<IAiServiceClient> _aiServiceClient = new();
    private readonly Mock<IEmailNotificationService> _emailNotificationService = new();

    public ProjectRiskAlertServiceTests()
    {
        _emailNotificationHistoryRepository
            .Setup(x => x.ExistsForProjectRiskOnDateAsync(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAtRiskProjects_DoesNotSendEmail()
    {
        var project = ApiTestData.CreateProject();
        project.HealthStatus = ManagerConstants.HealthOnTrack;

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAtRiskProjectExists_SendsEmailAndLogsHistory()
    {
        var project = ApiTestData.CreateProject();
        project.HealthStatus = ManagerConstants.HealthAtRisk;
        project.Milestones =
        [
            ApiTestData.CreateMilestone(title: "Release", status: MilestoneConstants.StatusInProgress),
        ];

        var riskFlags = new List<ProjectRiskFlag>
        {
            new()
            {
                Id = 1,
                ProjectId = project.Id,
                SortOrder = 1,
                Outcome = ManagerConstants.RiskFlagFail,
                Message = "Milestone overdue",
            },
        };

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _projectRiskFlagRepository
            .Setup(x => x.GetByProjectId(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskFlags);
        _aiServiceClient
            .Setup(x => x.GetRiskSummaryAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskSummaryResponse
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                HealthStatus = ManagerConstants.HealthAtRisk,
                Summary = "The project is behind schedule.",
            });
        _aiServiceClient
            .Setup(x => x.BuildTeamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeamBuilderResponse
            {
                Query = "test",
                Team =
                [
                    new TeamBuilderMember
                    {
                        Role = "Backend Developer",
                        ResourceUserId = 5,
                        Name = "Alex Bench",
                        SkillsMatch = "C#, .NET",
                        Reason = "Available on bench with matching skills.",
                    },
                ],
            });

        EmailMessage? capturedEmail = null;
        _emailNotificationService
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedEmail = message)
            .Returns(Task.CompletedTask);

        EmailNotificationHistory? capturedHistory = null;
        _emailNotificationHistoryRepository
            .Setup(x => x.Add(It.IsAny<EmailNotificationHistory>(), It.IsAny<CancellationToken>()))
            .Callback<EmailNotificationHistory, CancellationToken>((history, _) => capturedHistory = history)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.ExecuteAsync();

        Assert.NotNull(capturedEmail);
        Assert.Equal(project.ManagerUser.Email, capturedEmail!.ToEmail);
        Assert.Contains(project.Name, capturedEmail.Subject);

        Assert.NotNull(capturedHistory);
        Assert.Equal((int)EmailNotificationTypeEnum.ProjectRisk, capturedHistory!.EmailTypeId);
        Assert.Equal(project.Id, capturedHistory.ProjectId);
        Assert.Equal(project.ManagerUserId, capturedHistory.UserId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), capturedHistory.SentOnDate);
        Assert.Equal(project.ManagerUser.Email, capturedHistory.RecipientEmail);
        Assert.Equal(capturedEmail.Subject, capturedHistory.Subject);

        _emailNotificationHistoryRepository.Verify(
            x => x.SaveChanges(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadySentToday_SkipsProject()
    {
        var project = ApiTestData.CreateProject();
        project.HealthStatus = ManagerConstants.HealthAtRisk;

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _emailNotificationHistoryRepository
            .Setup(x => x.ExistsForProjectRiskOnDateAsync(
                project.Id,
                DateOnly.FromDateTime(DateTime.UtcNow),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailNotificationHistoryRepository.Verify(
            x => x.Add(It.IsAny<EmailNotificationHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenManagerInactive_SkipsProject()
    {
        var project = ApiTestData.CreateProject();
        project.HealthStatus = ManagerConstants.HealthAtRisk;
        project.ManagerUser.IsActive = false;

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _emailNotificationService.Verify(
            x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiServiceFails_StillSendsEmailWithFallbackText()
    {
        var project = ApiTestData.CreateProject();
        project.HealthStatus = ManagerConstants.HealthAtRisk;

        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });
        _projectRiskFlagRepository
            .Setup(x => x.GetByProjectId(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectRiskFlag>());
        _aiServiceClient
            .Setup(x => x.GetRiskSummaryAsync(project.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("AiService unavailable"));
        _aiServiceClient
            .Setup(x => x.BuildTeamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("AiService unavailable"));

        EmailMessage? capturedEmail = null;
        _emailNotificationService
            .Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => capturedEmail = message)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.ExecuteAsync();

        Assert.NotNull(capturedEmail);
        Assert.Contains(AppConstants.Email.AiRiskSummaryUnavailable, capturedEmail!.HtmlBody);
        Assert.Contains(AppConstants.Email.AiTeamSuggestionsUnavailable, capturedEmail.HtmlBody);
    }

    private ProjectRiskAlertService CreateSut() =>
        new(
            _projectRepository.Object,
            _projectRiskFlagRepository.Object,
            _emailNotificationHistoryRepository.Object,
            _aiServiceClient.Object,
            _emailNotificationService.Object,
            NullLogger<ProjectRiskAlertService>.Instance);
}
