using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Models.Skills;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class SkillService(
    IEmployeeRepository employeeRepository,
    ISkillRepository skillRepository,
    IEmployeeSkillRepository employeeSkillRepository,
    IMapper mapper) : ISkillService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ISkillRepository _skillRepository = skillRepository;
    private readonly IEmployeeSkillRepository _employeeSkillRepository = employeeSkillRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<EmployeeSkillsResult> GetForEmployee(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrow(employeeId, cancellationToken);
        var employeeSkills = await _employeeSkillRepository.GetByEmployeeId(employeeId, cancellationToken);

        return new EmployeeSkillsResult
        {
            EmployeeId = employee.Id,
            FullName = employee.User.FullName,
            Skills = _mapper.Map<IReadOnlyList<EmployeeSkillItem>>(employeeSkills),
        };
    }

    public async Task<int> Add(
        int employeeId,
        AddEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetEmployeeOrThrow(employeeId, cancellationToken);
        ValidateCategory(request.Category);
        var proficiency = NormalizeProficiency(request.Proficiency);

        var skill = await GetOrCreateSkill(request, cancellationToken);
        var key = new EmployeeSkillKey(employeeId, skill.Id);

        if (await _employeeSkillRepository.Exists(key, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Skills.SkillAlreadyAssigned);
        }

        var employeeSkill = _mapper.Map<EmployeeSkill>(request);
        employeeSkill.EmployeeId = employeeId;
        employeeSkill.SkillId = skill.Id;
        employeeSkill.Proficiency = proficiency;

        await _employeeSkillRepository.Add(employeeSkill, cancellationToken);
        await _employeeSkillRepository.SaveChanges(cancellationToken);

        return skill.Id;
    }

    public async Task<bool> Update(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Proficiency = NormalizeProficiency(request.Proficiency);
        var key = new EmployeeSkillKey(employeeId, skillId);
        var employeeSkill = await _employeeSkillRepository.GetById(key, cancellationToken);

        if (employeeSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.EmployeeSkillNotFound);
        }

        _mapper.Map(request, employeeSkill);
        _employeeSkillRepository.Update(employeeSkill);
        await _employeeSkillRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task Remove(int employeeId, int skillId, CancellationToken cancellationToken = default)
    {
        var key = new EmployeeSkillKey(employeeId, skillId);
        var employeeSkill = await _employeeSkillRepository.GetById(key, cancellationToken);

        if (employeeSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.EmployeeSkillNotFound);
        }

        _employeeSkillRepository.Remove(employeeSkill);
        await _employeeSkillRepository.SaveChanges(cancellationToken);
    }

    private async Task<Skill> GetOrCreateSkill(AddEmployeeSkillRequest request, CancellationToken cancellationToken)
    {
        var skillName = request.SkillName.Trim();
        var skill = await _skillRepository.GetByName(skillName, cancellationToken);

        if (skill is not null)
        {
            return skill;
        }

        skill = new Skill
        {
            Name = skillName,
            Category = request.Category.Trim(),
        };

        await _skillRepository.Add(skill, cancellationToken);
        await _skillRepository.SaveChanges(cancellationToken);

        return skill;
    }

    private async Task<Employee> GetEmployeeOrThrow(int employeeId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetById(employeeId, cancellationToken);
        if (employee is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return employee;
    }

    private static void ValidateCategory(string category)
    {
        if (!SkillConstants.ValidCategories.Contains(category.Trim()))
        {
            throw new ArgumentException(AppConstants.Skills.InvalidCategory);
        }
    }

    private static string NormalizeProficiency(string proficiency)
    {
        var trimmed = proficiency.Trim();
        var match = SkillConstants.ValidProficiencies.FirstOrDefault(
            x => x.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            throw new ArgumentException(AppConstants.Skills.InvalidProficiency);
        }

        return match;
    }
}
