using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models;
using Prm.Common.Models.Projects;
using Prm.Common.Models.Manager;

namespace Prm.Api.Tests.Controllers;

public class ProjectControllerTests
{
    private readonly Mock<IProjectService> _projectService = new();
    private const int ManagerUserId = 10;

    [Fact]
    public async Task Add_WhenValid_ReturnsCreated()
    {
        _projectService
            .Setup(x => x.Add(It.IsAny<CreateProjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var sut = CreateSut();
        var result = await sut.Add(
            new CreateProjectRequest
            {
                Name = "Alpha",
                Description = "Desc",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                Status = 1,
                ManagerUserId = ManagerUserId,
            },
            CancellationToken.None);

        Assert.Equal(42, ControllerTestHelper.AssertCreatedValue<CreatedIdResponse>(result).Id);
    }

    [Fact]
    public async Task GetProjects_WhenProjectsExist_ReturnsOk()
    {
        var response = new ProjectListResult
        {
            Projects = [new ProjectSummary { Id = 1, Name = "Alpha" }],
        };

        _projectService
            .Setup(x => x.GetProjects(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetProjects(CancellationToken.None);

        Assert.Single(ControllerTestHelper.AssertOkValue<ProjectListResult>(result).Projects);
    }

    [Fact]
    public async Task Update_WhenProjectNotFound_Returns404()
    {
        _projectService
            .Setup(x => x.Update(99, It.IsAny<UpdateProjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Projects.NotFound));

        var sut = CreateSut();
        var result = await sut.Update(
            99,
            new UpdateProjectRequest
            {
                Name = "Beta",
                Description = "Desc",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                Status = 1,
                ManagerUserId = ManagerUserId,
            },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetMyProjects_WhenManagerHasProjects_ReturnsOk()
    {
        var response = new ManagerProjectListResult
        {
            Projects = [new ManagerProjectSummary { Id = 1, Name = "Alpha" }],
        };

        _projectService
            .Setup(x => x.GetMyProjects(ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetMyProjects(CancellationToken.None);

        Assert.Single(ControllerTestHelper.AssertOkValue<ManagerProjectListResult>(result).Projects);
    }

    [Fact]
    public async Task GetDetail_WhenValid_ReturnsOk()
    {
        var response = new ManagerProjectDetailResponse { Id = 1, Name = "Alpha" };

        _projectService
            .Setup(x => x.GetProjectDetail(1, ManagerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = CreateSut();
        var result = await sut.GetDetail(1, CancellationToken.None);

        Assert.Equal("Alpha", ControllerTestHelper.AssertOkValue<ManagerProjectDetailResponse>(result).Name);
    }

    private ProjectController CreateSut() =>
        new(
            _projectService.Object,
            ControllerTestHelper.CreateManagerAccess(
                ManagerUserId,
                ApiTestData.CreateUser(ManagerUserId, (int)RoleNameEnum.Manager)));
}
