using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Milestones;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class MilestoneServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IMilestoneRepository> _milestoneRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task GetByProjectId_WhenProjectNotFound_ThrowsKeyNotFoundException()
    {
        _projectRepository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetByProjectId(1));

        Assert.Equal(AppConstants.Projects.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetByProjectId_WhenSuccessful_ReturnsProjectAndMilestones()
    {
        var project = ApiTestData.CreateProject();
        var milestones = new List<Milestone> { ApiTestData.CreateMilestone() };

        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.GetByProjectId(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);

        var sut = CreateSut();
        var result = await sut.GetByProjectId(project.Id);

        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(project.Name, result.ProjectName);
        Assert.Single(result.Milestones);
    }

    [Fact]
    public async Task Add_WhenTitleExists_ThrowsInvalidOperationException()
    {
        var project = ApiTestData.CreateProject();
        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.ExistsByTitleForProject(project.Id, "Phase 1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(project.Id, new AddMilestoneRequest
            {
                Title = "Phase 1",
                DueDate = new DateOnly(2026, 6, 1),
                Status = (int)MilestoneStatusEnum.NotStarted,
            }));

        Assert.Equal(AppConstants.Milestones.TitleExists, exception.Message);
    }

    [Fact]
    public async Task Add_WhenDueDateBeforeProjectStart_ThrowsArgumentException()
    {
        var project = ApiTestData.CreateProject();
        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.ExistsByTitleForProject(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(project.Id, new AddMilestoneRequest
            {
                Title = "Early",
                DueDate = project.StartDate.AddDays(-1),
                Status = (int)MilestoneStatusEnum.NotStarted,
            }));

        Assert.Equal(AppConstants.Milestones.InvalidDueDate, exception.Message);
    }

    [Fact]
    public async Task Add_WhenDueDateAfterProjectEnd_ThrowsArgumentException()
    {
        var project = ApiTestData.CreateProject();
        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.ExistsByTitleForProject(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(project.Id, new AddMilestoneRequest
            {
                Title = "Late",
                DueDate = project.EndDate.AddDays(1),
                Status = (int)MilestoneStatusEnum.NotStarted,
            }));

        Assert.Equal(AppConstants.Milestones.InvalidDueDate, exception.Message);
    }

    [Fact]
    public async Task Add_WhenSuccessful_ReturnsMilestoneId()
    {
        var project = ApiTestData.CreateProject();
        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.ExistsByTitleForProject(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _milestoneRepository
            .Setup(x => x.Add(It.IsAny<Milestone>(), It.IsAny<CancellationToken>()))
            .Callback<Milestone, CancellationToken>((milestone, _) => milestone.Id = 3)
            .Returns(Task.CompletedTask);
        _milestoneRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var id = await sut.Add(project.Id, new AddMilestoneRequest
        {
            Title = "Delivery",
            DueDate = new DateOnly(2026, 6, 1),
            Status = (int)MilestoneStatusEnum.InProgress,
        });

        Assert.Equal(3, id);
    }

    [Fact]
    public async Task Update_WhenMilestoneNotFound_ThrowsKeyNotFoundException()
    {
        var project = ApiTestData.CreateProject();
        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.GetByIdAndProjectId(1, project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Milestone?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(project.Id, 1, new UpdateMilestoneRequest { Status = (int)MilestoneStatusEnum.Done }));

        Assert.Equal(AppConstants.Milestones.NotFound, exception.Message);
    }

    [Fact]
    public async Task Update_WhenInvalidStatus_ThrowsArgumentException()
    {
        var project = ApiTestData.CreateProject();
        var milestone = ApiTestData.CreateMilestone(projectId: project.Id);

        _projectRepository
            .Setup(x => x.GetById(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepository
            .Setup(x => x.GetByIdAndProjectId(milestone.Id, project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestone);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Update(project.Id, milestone.Id, new UpdateMilestoneRequest { Status = 99 }));

        Assert.Equal(AppConstants.Milestones.InvalidStatus, exception.Message);
    }

    private MilestoneService CreateSut() =>
        new(_projectRepository.Object, _milestoneRepository.Object, _mapper);
}
