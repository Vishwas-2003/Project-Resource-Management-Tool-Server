using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.Milestones;

namespace Prm.Api.Tests.Controllers;

public class MilestoneControllerTests
{
    private readonly Mock<IMilestoneService> _milestoneService = new();

    [Fact]
    public async Task GetByProject_WhenMilestonesExist_ReturnsOk()
    {
        var response = new ProjectMilestonesResult
        {
            ProjectId = 1,
            Milestones = [new MilestoneSummary { Id = 1, Title = "Phase 1" }],
        };

        _milestoneService
            .Setup(x => x.GetByProjectId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new MilestoneController(_milestoneService.Object);
        var result = await sut.GetByProject(1, CancellationToken.None);

        Assert.Single(ControllerTestHelper.AssertOkValue<ProjectMilestonesResult>(result).Milestones);
    }

    [Fact]
    public async Task Add_WhenValid_ReturnsCreated()
    {
        _milestoneService
            .Setup(x => x.Add(1, It.IsAny<AddMilestoneRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var sut = new MilestoneController(_milestoneService.Object);
        var result = await sut.Add(
            1,
            new AddMilestoneRequest { Title = "Phase 1", DueDate = new DateOnly(2026, 6, 1) },
            CancellationToken.None);

        Assert.Equal(7, ControllerTestHelper.AssertCreatedValue<CreatedIdResponse>(result).Id);
    }

    [Fact]
    public async Task Add_WhenProjectNotFound_Returns404()
    {
        _milestoneService
            .Setup(x => x.Add(1, It.IsAny<AddMilestoneRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Projects.NotFound));

        var sut = new MilestoneController(_milestoneService.Object);
        var result = await sut.Add(
            1,
            new AddMilestoneRequest { Title = "Phase 1", DueDate = new DateOnly(2026, 6, 1) },
            CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }

    [Fact]
    public async Task Update_WhenValid_ReturnsOk()
    {
        _milestoneService
            .Setup(x => x.Update(1, 2, It.IsAny<UpdateMilestoneRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new MilestoneController(_milestoneService.Object);
        var result = await sut.Update(
            1,
            2,
            new UpdateMilestoneRequest
            {
                Title = "Phase 2",
                DueDate = new DateOnly(2026, 7, 1),
                StoryPoints = 5,
                Status = 1,
            },
            CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }
}
