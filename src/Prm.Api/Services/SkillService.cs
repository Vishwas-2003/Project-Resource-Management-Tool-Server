using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Models.Skills;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class SkillService(
    IUserRepository _userRepository,
    ISkillRepository _skillRepository,
    IUserSkillRepository _userSkillRepository,
    IMapper _mapper) : ISkillService
{
    public async Task<EmployeeSkillsResult> GetForEmployee(int employeeId, CancellationToken cancellationToken = default)
    {
        var user = await GetEmployeeUserOrThrow(employeeId, cancellationToken);
        var userSkills = await _userSkillRepository.GetByUserId(employeeId, cancellationToken);

        var skills = _mapper.Map<List<EmployeeSkillItem>>(userSkills);
        for (var rowIndex = 0; rowIndex < skills.Count; rowIndex++)
        {
            skills[rowIndex].RowNumber = rowIndex + 1;
        }

        return new EmployeeSkillsResult
        {
            EmployeeId = user.Id,
            FullName = user.FullName,
            Skills = skills,
        };
    }

    public async Task<int> Add(
        int employeeId,
        AddEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetEmployeeUserOrThrow(employeeId, cancellationToken);
        ValidateCategory(request.Category);
        var proficiency = NormalizeProficiency(request.Proficiency);

        var skill = await GetOrCreateSkill(request, cancellationToken);

        if (await _userSkillRepository.Exists(employeeId, skill.Id, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Skills.SkillAlreadyAssigned);
        }

        var userSkill = _mapper.Map<UserSkill>(request);
        userSkill.UserId = employeeId;
        userSkill.SkillId = skill.Id;
        userSkill.Proficiency = proficiency;

        await _userSkillRepository.Add(userSkill, cancellationToken);
        await _userSkillRepository.SaveChanges(cancellationToken);

        return skill.Id;
    }

    public async Task<bool> Update(
        int employeeId,
        int skillId,
        UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Proficiency = NormalizeProficiency(request.Proficiency);
        var key = new UserSkillKey(employeeId, skillId);
        var userSkill = await _userSkillRepository.GetById(key, cancellationToken);

        if (userSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.EmployeeSkillNotFound);
        }

        _mapper.Map(request, userSkill);
        _userSkillRepository.Update(userSkill);
        await _userSkillRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task Remove(int employeeId, int skillId, CancellationToken cancellationToken = default)
    {
        var key = new UserSkillKey(employeeId, skillId);
        var userSkill = await _userSkillRepository.GetById(key, cancellationToken);

        if (userSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.EmployeeSkillNotFound);
        }

        _userSkillRepository.Remove(userSkill);
        await _userSkillRepository.SaveChanges(cancellationToken);
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

    private async Task<User> GetEmployeeUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Employees.NotFound);
        }

        return user;
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
