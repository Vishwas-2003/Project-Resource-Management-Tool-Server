using AutoMapper;
using Moq;
using Prm.Api.Services;
using Prm.Common.Constants;
using Prm.Common.Models.Skills;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Api.Tests.Helpers;

namespace Prm.Api.Tests.Services;

public class SkillServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ISkillRepository> _skillRepository = new();
    private readonly Mock<IUserSkillRepository> _userSkillRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task GetForEmployee_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _userRepository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetForEmployee(1));

        Assert.Equal(AppConstants.Employees.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetForEmployee_WhenSuccessful_ReturnsSkills()
    {
        var user = ApiTestData.CreateEmployeeUser();
        var userSkills = new List<UserSkill>
        {
            new()
            {
                UserId = user.Id,
                SkillId = 1,
                Proficiency = SkillConstants.ProficiencyIntermediate,
                Skill = new Skill { Id = 1, Name = "C#", Category = "Backend" },
            },
        };

        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userSkillRepository
            .Setup(x => x.GetByUserId(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSkills);

        var sut = CreateSut();
        var result = await sut.GetForEmployee(user.Id);

        Assert.Equal(user.Id, result.EmployeeUserId);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Single(result.Skills);
        Assert.Equal("C#", result.Skills[0].SkillName);
    }

    [Fact]
    public async Task Add_WhenInvalidCategory_ThrowsArgumentException()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(user.Id, new AddEmployeeSkillRequest
            {
                SkillName = "C#",
                Category = "Invalid",
                Proficiency = SkillConstants.ProficiencyBeginner,
            }));

        Assert.Equal(AppConstants.Skills.InvalidCategory, exception.Message);
    }

    [Fact]
    public async Task Add_WhenInvalidProficiency_ThrowsArgumentException()
    {
        var user = ApiTestData.CreateEmployeeUser();
        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(user.Id, new AddEmployeeSkillRequest
            {
                SkillName = "C#",
                Category = "Backend",
                Proficiency = "Expert",
            }));

        Assert.Equal(AppConstants.Skills.InvalidProficiency, exception.Message);
    }

    [Fact]
    public async Task Add_WhenSkillAlreadyAssigned_ThrowsInvalidOperationException()
    {
        var user = ApiTestData.CreateEmployeeUser();
        var skill = new Skill { Id = 5, Name = "C#", Category = "Backend" };

        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _skillRepository
            .Setup(x => x.GetByName("C#", It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _userSkillRepository
            .Setup(x => x.Exists(user.Id, skill.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(user.Id, new AddEmployeeSkillRequest
            {
                SkillName = "C#",
                Category = "Backend",
                Proficiency = SkillConstants.ProficiencyBeginner,
            }));

        Assert.Equal(AppConstants.Skills.SkillAlreadyAssigned, exception.Message);
    }

    [Fact]
    public async Task Add_WhenSkillDoesNotExist_CreatesSkillAndReturnsSkillId()
    {
        var user = ApiTestData.CreateEmployeeUser();

        _userRepository
            .Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _skillRepository
            .Setup(x => x.GetByName("Go", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Skill?)null);
        _skillRepository
            .Setup(x => x.Add(It.IsAny<Skill>(), It.IsAny<CancellationToken>()))
            .Callback<Skill, CancellationToken>((skill, _) => skill.Id = 8)
            .Returns(Task.CompletedTask);
        _skillRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userSkillRepository
            .Setup(x => x.Exists(user.Id, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userSkillRepository
            .Setup(x => x.Add(It.IsAny<UserSkill>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userSkillRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var skillId = await sut.Add(user.Id, new AddEmployeeSkillRequest
        {
            SkillName = "Go",
            Category = "Backend",
            Proficiency = "advanced",
        });

        Assert.Equal(8, skillId);
        _skillRepository.Verify(x => x.Add(It.Is<Skill>(s => s.Name == "Go"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenSuccessful_ReturnsTrue()
    {
        var user = ApiTestData.CreateEmployeeUser();
        var assignment = new UserSkill
        {
            UserId = user.Id,
            SkillId = 1,
            Proficiency = SkillConstants.ProficiencyBeginner,
            Skill = new Skill { Id = 1, Name = "C#", Category = "Backend" },
        };

        _userSkillRepository
            .Setup(x => x.GetById(It.IsAny<UserSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _userSkillRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Update(
            user.Id,
            1,
            new UpdateEmployeeSkillRequest { Proficiency = SkillConstants.ProficiencyAdvanced });

        Assert.True(result);
        Assert.Equal(SkillConstants.ProficiencyAdvanced, assignment.Proficiency);
    }

    [Fact]
    public async Task Update_WhenAssignmentNotFound_ThrowsKeyNotFoundException()
    {
        _userSkillRepository
            .Setup(x => x.GetById(It.IsAny<UserSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, 1, new UpdateEmployeeSkillRequest { Proficiency = SkillConstants.ProficiencyAdvanced }));

        Assert.Equal(AppConstants.Skills.EmployeeSkillNotFound, exception.Message);
    }

    [Fact]
    public async Task Remove_WhenAssignmentNotFound_ThrowsKeyNotFoundException()
    {
        _userSkillRepository
            .Setup(x => x.GetById(It.IsAny<UserSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSkill?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Remove(1, 1));

        Assert.Equal(AppConstants.Skills.EmployeeSkillNotFound, exception.Message);
    }

    [Fact]
    public async Task Remove_WhenSuccessful_RemovesAssignment()
    {
        var userSkill = new UserSkill
        {
            UserId = 1,
            SkillId = 2,
            Proficiency = SkillConstants.ProficiencyBeginner,
        };

        _userSkillRepository
            .Setup(x => x.GetById(It.IsAny<UserSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSkill);

        var sut = CreateSut();
        await sut.Remove(1, 2);

        _userSkillRepository.Verify(x => x.Remove(userSkill), Times.Once);
        _userSkillRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    private SkillService CreateSut() =>
        new(
            _userRepository.Object,
            _skillRepository.Object,
            _userSkillRepository.Object,
            _mapper);
}
