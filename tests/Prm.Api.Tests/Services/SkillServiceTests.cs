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
    private readonly Mock<IEmployeeRepository> _employeeRepository = new();
    private readonly Mock<ISkillRepository> _skillRepository = new();
    private readonly Mock<IEmployeeSkillRepository> _employeeSkillRepository = new();
    private readonly IMapper _mapper = MapperTestHelper.CreateMapper();

    [Fact]
    public async Task GetForEmployee_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
    {
        _employeeRepository
            .Setup(x => x.GetById(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetForEmployee(1));

        Assert.Equal(AppConstants.Employees.NotFound, exception.Message);
    }

    [Fact]
    public async Task GetForEmployee_WhenSuccessful_ReturnsSkills()
    {
        var employee = ApiTestData.CreateEmployee();
        var employeeSkills = new List<EmployeeSkill>
        {
            new()
            {
                EmployeeId = employee.Id,
                SkillId = 1,
                Proficiency = SkillConstants.ProficiencyIntermediate,
                Skill = new Skill { Id = 1, Name = "C#", Category = "Backend" },
            },
        };

        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _employeeSkillRepository
            .Setup(x => x.GetByEmployeeId(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeSkills);

        var sut = CreateSut();
        var result = await sut.GetForEmployee(employee.Id);

        Assert.Equal(employee.Id, result.EmployeeId);
        Assert.Equal(employee.User.FullName, result.FullName);
        Assert.Single(result.Skills);
        Assert.Equal("C#", result.Skills[0].SkillName);
    }

    [Fact]
    public async Task Add_WhenInvalidCategory_ThrowsArgumentException()
    {
        var employee = ApiTestData.CreateEmployee();
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(employee.Id, new AddEmployeeSkillRequest
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
        var employee = ApiTestData.CreateEmployee();
        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.Add(employee.Id, new AddEmployeeSkillRequest
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
        var employee = ApiTestData.CreateEmployee();
        var skill = new Skill { Id = 5, Name = "C#", Category = "Backend" };

        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _skillRepository
            .Setup(x => x.GetByName("C#", It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _employeeSkillRepository
            .Setup(x => x.Exists(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Add(employee.Id, new AddEmployeeSkillRequest
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
        var employee = ApiTestData.CreateEmployee();

        _employeeRepository
            .Setup(x => x.GetById(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
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
        _employeeSkillRepository
            .Setup(x => x.Exists(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _employeeSkillRepository
            .Setup(x => x.Add(It.IsAny<EmployeeSkill>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _employeeSkillRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var skillId = await sut.Add(employee.Id, new AddEmployeeSkillRequest
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
        var employee = ApiTestData.CreateEmployee();
        var assignment = new EmployeeSkill
        {
            EmployeeId = employee.Id,
            SkillId = 1,
            Proficiency = SkillConstants.ProficiencyBeginner,
            Skill = new Skill { Id = 1, Name = "C#", Category = "Backend" },
        };

        _employeeSkillRepository
            .Setup(x => x.GetById(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);
        _employeeSkillRepository
            .Setup(x => x.SaveChanges(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.Update(
            employee.Id,
            1,
            new UpdateEmployeeSkillRequest { Proficiency = SkillConstants.ProficiencyAdvanced });

        Assert.True(result);
        Assert.Equal(SkillConstants.ProficiencyAdvanced, assignment.Proficiency);
    }

    [Fact]
    public async Task Update_WhenAssignmentNotFound_ThrowsKeyNotFoundException()
    {
        _employeeSkillRepository
            .Setup(x => x.GetById(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeSkill?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.Update(1, 1, new UpdateEmployeeSkillRequest { Proficiency = SkillConstants.ProficiencyAdvanced }));

        Assert.Equal(AppConstants.Skills.EmployeeSkillNotFound, exception.Message);
    }

    [Fact]
    public async Task Remove_WhenAssignmentNotFound_ThrowsKeyNotFoundException()
    {
        _employeeSkillRepository
            .Setup(x => x.GetById(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeSkill?)null);

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.Remove(1, 1));

        Assert.Equal(AppConstants.Skills.EmployeeSkillNotFound, exception.Message);
    }

    [Fact]
    public async Task Remove_WhenSuccessful_RemovesAssignment()
    {
        var employeeSkill = new EmployeeSkill
        {
            EmployeeId = 1,
            SkillId = 2,
            Proficiency = SkillConstants.ProficiencyBeginner,
        };

        _employeeSkillRepository
            .Setup(x => x.GetById(It.IsAny<EmployeeSkillKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeSkill);

        var sut = CreateSut();
        await sut.Remove(1, 2);

        _employeeSkillRepository.Verify(x => x.Remove(employeeSkill), Times.Once);
        _employeeSkillRepository.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
    }

    private SkillService CreateSut() =>
        new(
            _employeeRepository.Object,
            _skillRepository.Object,
            _employeeSkillRepository.Object,
            _mapper);
}
