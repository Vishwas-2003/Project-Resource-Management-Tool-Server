using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Projects;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IProjectRiskFlagRepository> _projectRiskFlagRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    private const int ManagerUserId = 10;

    private static DateOnly Start => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
    private static DateOnly End => Start.AddMonths(12);

    [Fact]
    public async Task Add_WhenPastStartDate_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var pastStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(new CreateProjectRequest
            {
                Name = "Beta",
                Description = "Desc",
                StartDate = pastStart,
                EndDate = End,
                Status = (int)ProjectStatusEnum.Planned,
                ManagerUserId = 10,
            }));

        Assert.Equal(AppConstants.Projects.PastDateNotAllowed, exception.Message);
    }

    [Fact]
    public async Task Add_WhenEndDateBeforeStartDate_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(new CreateProjectRequest
            {
                Name = "Beta",
                Description = "Desc",
                StartDate = End,
                EndDate = Start,
                Status = (int)ProjectStatusEnum.Planned,
                ManagerUserId = 10,
            }));

        Assert.Equal(AppConstants.Projects.InvalidDateRange, exception.Message);
    }

    [Fact]
    public async Task Add_WhenManagerNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Add(CreateValidRequest()));

        Assert.Equal(AppConstants.Projects.ManagerNotFound, exception.Message);
    }

    [Fact]
    public async Task Add_WhenNameExists_ThrowsInvalidOperationException()
    {
        SetupValidManager();
        _projectRepository
            .Setup(x => x.ExistsByName("Alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(CreateValidRequest(name: "Alpha")));

        Assert.Equal(AppConstants.Projects.NameExists, exception.Message);
    }

    [Fact]
    public async Task Add_WhenInvalidStatus_ThrowsArgumentException()
    {
        SetupValidManager();
        _projectRepository
            .Setup(x => x.ExistsByName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(CreateValidRequest(status: 99)));

        Assert.Equal(AppConstants.Projects.InvalidStatus, exception.Message);
    }

    [Fact]
    public async Task Add_WhenSuccessful_ReturnsProjectId()
    {
        SetupValidManager();
        _projectRepository
            .Setup(x => x.ExistsByName(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _projectRepository
            .Setup(x => x.Add(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => project.Id = 5)
            .Returns(Task.CompletedTask);
        _projectRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var id = await sut.Add(CreateValidRequest());

        Assert.Equal(5, id);
    }

    [Fact]
    public async Task Update_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        SetupValidManager();
        _projectRepository
            .Setup(x => x.GetByIdWithManager(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, CreateUpdateRequest()));

        Assert.Equal(AppConstants.Projects.NotFound, exception.Message);
    }

    [Fact]
    public async Task Update_WhenNameExistsForAnotherProject_ThrowsInvalidOperationException()
    {
        var project = ApiTestData.CreateProject();
        SetupValidManager();
        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _projectRepository
            .Setup(x => x.ExistsByName("Taken", project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Update(project.Id, CreateUpdateRequest(name: "Taken")));

        Assert.Equal(AppConstants.Projects.NameExists, exception.Message);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsTrue()
    {
        var project = ApiTestData.CreateProject();
        SetupValidManager();
        _projectRepository
            .Setup(x => x.GetByIdWithManager(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _projectRepository
            .Setup(x => x.ExistsByName(It.IsAny<string>(), project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        var result = await sut.Update(project.Id, CreateUpdateRequest(name: "Renamed"));

        Assert.True(result);
        Assert.Equal("Renamed", project.Name);
        Assert.Equal(ProjectConstants.StatusActive, project.Status);
    }

    [Fact]
    public async Task GetProjects_ReturnsMappedSummaries()
    {
        var projects = new List<Project> { ApiTestData.CreateProject() };
        _projectRepository
            .Setup(x => x.GetAllWithManager(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var sut = CreateSut();
        var result = await sut.GetProjects();

        Assert.Single(result.Projects);
        Assert.Equal("Alpha", result.Projects[0].Name);
    }

    [Fact]
    public async Task GetMyProjects_WhenManagerNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetMyProjects(ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProfileNotFound, exception.Message);
    }

    [Fact]
    public async Task GetMyProjects_ReturnsMappedProjectsWithRowNumbers()
    {
        SetupValidManager();
        var projects = new List<Project>
        {
            ApiTestData.CreateProject(id: 1, name: "Alpha"),
            ApiTestData.CreateProject(id: 2, name: "Beta", start: new DateOnly(2026, 2, 1), end: new DateOnly(2026, 8, 31)),
        };
        _projectRepository
            .Setup(x => x.GetByManagerUserId(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var sut = CreateSut();
        var result = await sut.GetMyProjects(ManagerUserId);

        Assert.Equal(2, result.Projects.Count);
        Assert.Equal(1, result.Projects[0].RowNumber);
        Assert.Equal("Alpha", result.Projects[0].Name);
        Assert.Equal(2, result.Projects[1].RowNumber);
        Assert.Equal("Beta", result.Projects[1].Name);
    }

    [Fact]
    public async Task GetProjectDetail_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        SetupValidManager();
        _projectRepository
            .Setup(x => x.GetByIdWithDetails(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetProjectDetail(99, ManagerUserId));

        Assert.Equal(AppConstants.Projects.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetProjectDetail_WhenProjectNotOwnedByManager_ThrowsUnauthorizedAccessException()
    {
        SetupValidManager();
        var project = ApiTestData.CreateProject();
        project.ManagerUserId = 99;
        _projectRepository
            .Setup(x => x.GetByIdWithDetails(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.GetProjectDetail(project.Id, ManagerUserId));

        Assert.Equal(AppConstants.Manager.ProjectNotOwned, exception.Message);
    }

    [Fact]
    public async Task GetProjectDetail_ReturnsDetailWithMilestonesAndRiskFlags()
    {
        SetupValidManager();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var project = ApiTestData.CreateProject();
        project.Milestones =
        [
            ApiTestData.CreateMilestone(id: 1, title: "Phase 1", dueDate: today.AddMonths(1)),
            ApiTestData.CreateMilestone(id: 2, title: "Phase 2", dueDate: today.AddMonths(2), status: MilestoneConstants.StatusDone),
        ];
        var allocatedEmployee = ApiTestData.CreateResourceUser(id: 5, fullName: "Bob Smith");
        project.Allocations =
        [
            ApiTestData.CreateAllocation(id: 10, userId: 5, projectId: project.Id, user: allocatedEmployee, project: project),
        ];

        var riskFlags = new List<ProjectRiskFlag>
        {
            new()
            {
                Id = 1,
                ProjectId = project.Id,
                SortOrder = 1,
                Outcome = ManagerConstants.RiskFlagFail,
                Message = "Resource shortfall",
            },
        };

        _projectRepository
            .Setup(x => x.GetByIdWithDetails(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _projectRiskFlagRepository
            .Setup(x => x.GetByProjectId(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(riskFlags);

        var sut = CreateSut();
        var result = await sut.GetProjectDetail(project.Id, ManagerUserId);

        Assert.Equal(project.Id, result.Id);
        Assert.Equal(project.Name, result.Name);
        Assert.Equal(project.HealthStatus, result.HealthStatus);
        Assert.Equal(2, result.Milestones.Count);
        Assert.Equal(1, result.Milestones[0].RowNumber);
        Assert.Equal("Phase 1", result.Milestones[0].Title);
        Assert.Single(result.RiskFlags);
        Assert.Equal(ManagerConstants.RiskFlagFail, result.RiskFlags[0].Outcome);
        Assert.Equal("Resource shortfall", result.RiskFlags[0].Message);
        Assert.Single(result.AllocatedResources);
        Assert.Equal("Bob Smith", result.AllocatedResources[0].Name);
    }

    private void SetupValidManager()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateUser(id: ManagerUserId, roleId: (int)RoleNameEnum.Manager, username: "manager"));
    }

    private static CreateProjectRequest CreateValidRequest(string name = "New Project", int status = (int)ProjectStatusEnum.Planned) =>
        new()
        {
            Name = name,
            Description = "Description",
            StartDate = Start,
            EndDate = End,
            Status = status,
            ManagerUserId = 10,
            TotalStoryPoints = 120,
        };

    private static UpdateProjectRequest CreateUpdateRequest(string name = "Updated") =>
        new()
        {
            Name = name,
            Description = "Updated desc",
            StartDate = Start,
            EndDate = End,
            Status = (int)ProjectStatusEnum.Active,
            ManagerUserId = 10,
            TotalStoryPoints = 120,
        };

    private ProjectService CreateSut() =>
        new(
            _projectRepository.Object,
            _projectRiskFlagRepository.Object,
            _userRepository.Object,
            _mapper);
}
