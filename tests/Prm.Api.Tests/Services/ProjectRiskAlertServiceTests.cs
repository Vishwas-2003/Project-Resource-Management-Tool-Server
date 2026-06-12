using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Api.Models.Ai;
using Prm.Api.Models.Email;
using Prm.Api.Services;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class ProjectRiskAlertServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IProjectRiskFlagRepository> _projectRiskFlagRepository = new();
    private readonly Mock<IAiServiceClient> _aiServiceClient = new();
    private readonly Mock<IEmailNotificationService> _emailNotificationService = new();

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
    public async Task ExecuteAsync_WhenAtRiskProjectExists_SendsEmailToManager()
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

        var sut = CreateSut();
        await sut.ExecuteAsync();

        Assert.NotNull(capturedEmail);
        Assert.Equal(project.ManagerUser.Email, capturedEmail!.ToEmail);
        Assert.Contains(project.Name, capturedEmail.Subject);
        Assert.Contains("The project is behind schedule.", capturedEmail.HtmlBody);
        Assert.Contains("Alex Bench", capturedEmail.HtmlBody);
        Assert.Contains("Milestone overdue", capturedEmail.HtmlBody);
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
            _aiServiceClient.Object,
            _emailNotificationService.Object,
            NullLogger<ProjectRiskAlertService>.Instance);
}
