using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Projects;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepository = new();
    private readonly Mock<ISystemConfigurationRepository> _systemConfigurationRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 12, 31);

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

    private void SetupValidManager()
    {
        _userRepository
            .Setup(x => x.GetActiveManagerById(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiTestData.CreateUser(id: 10, roleId: (int)RoleNameEnum.Manager, username: "manager"));
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
        };

    private ProjectService CreateSut() =>
        new(
            _projectRepository.Object,
            _userRepository.Object,
            _timesheetRepository.Object,
            _systemConfigurationRepository.Object,
            _mapper);
}
