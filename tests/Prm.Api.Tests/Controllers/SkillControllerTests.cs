using Microsoft.AspNetCore.Http;
using Moq;
using Prm.Api.Controllers;
using Prm.Api.Services.Interfaces;
using Prm.Api.Tests.Helpers;
using Prm.Common.Constants;
using Prm.Common.Models;
using Prm.Common.Models.Skills;

namespace Prm.Api.Tests.Controllers;

public class SkillControllerTests
{
    private readonly Mock<ISkillService> _skillService = new();

    [Fact]
    public async Task GetForEmployee_WhenSkillsExist_ReturnsOk()
    {
        var response = new EmployeeSkillsResult
        {
            EmployeeUserId = 1,
            Skills = [new EmployeeSkillItem { SkillId = 1, SkillName = "C#" }],
        };

        _skillService
            .Setup(x => x.GetForEmployee(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new SkillController(_skillService.Object);
        var result = await sut.GetForEmployee(1, CancellationToken.None);

        Assert.Single(ControllerTestHelper.AssertOkValue<EmployeeSkillsResult>(result).Skills);
    }

    [Fact]
    public async Task Add_WhenValid_ReturnsCreated()
    {
        _skillService
            .Setup(x => x.Add(1, It.IsAny<AddEmployeeSkillRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var sut = new SkillController(_skillService.Object);
        var result = await sut.Add(
            1,
            new AddEmployeeSkillRequest { SkillName = "C#", Category = "Backend", Proficiency = "Intermediate" },
            CancellationToken.None);

        Assert.Equal(5, ControllerTestHelper.AssertCreatedValue<CreatedIdResponse>(result).Id);
    }

    [Fact]
    public async Task Update_WhenValid_ReturnsOk()
    {
        _skillService
            .Setup(x => x.Update(1, 2, It.IsAny<UpdateEmployeeSkillRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new SkillController(_skillService.Object);
        var result = await sut.Update(
            1,
            2,
            new UpdateEmployeeSkillRequest { Proficiency = "Advanced" },
            CancellationToken.None);

        Assert.True(ControllerTestHelper.AssertOkValue<UpdatedResponse>(result).Updated);
    }

    [Fact]
    public async Task Remove_WhenSkillNotFound_Returns404()
    {
        _skillService
            .Setup(x => x.Remove(1, 99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException(AppConstants.Skills.EmployeeSkillNotFound));

        var sut = new SkillController(_skillService.Object);
        var result = await sut.Remove(1, 99, CancellationToken.None);

        ControllerTestHelper.AssertErrorResult(
            result,
            StatusCodes.Status404NotFound,
            AppConstants.ErrorCodes.NotFound);
    }
}
