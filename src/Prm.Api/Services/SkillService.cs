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
    public async Task<ResourceSkillsResult> GetForResource(int resourceUserId, CancellationToken cancellationToken = default)
    {
        var user = await GetResourceUserOrThrow(resourceUserId, cancellationToken);
        var userSkills = await _userSkillRepository.GetByUserId(resourceUserId, cancellationToken);

        var skills = _mapper.Map<List<ResourceSkillItem>>(userSkills);
        for (var rowIndex = 0; rowIndex < skills.Count; rowIndex++)
        {
            skills[rowIndex].RowNumber = rowIndex + 1;
        }

        return new ResourceSkillsResult
        {
            ResourceUserId = user.Id,
            FullName = user.FullName,
            Skills = skills,
        };
    }

    public async Task<int> Add(
        int resourceUserId,
        AddResourceSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetResourceUserOrThrow(resourceUserId, cancellationToken);
        ValidateCategory(request.Category);
        var proficiency = NormalizeProficiency(request.Proficiency);

        var skill = await GetOrCreateSkill(request, cancellationToken);

        if (await _userSkillRepository.Exists(resourceUserId, skill.Id, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Skills.SkillAlreadyAssigned);
        }

        var userSkill = _mapper.Map<UserSkill>(request);
        userSkill.UserId = resourceUserId;
        userSkill.SkillId = skill.Id;
        userSkill.Proficiency = proficiency;

        await _userSkillRepository.Add(userSkill, cancellationToken);
        await _userSkillRepository.SaveChanges(cancellationToken);

        return skill.Id;
    }

    public async Task<bool> Update(
        int resourceUserId,
        int skillId,
        UpdateResourceSkillRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Proficiency = NormalizeProficiency(request.Proficiency);
        var key = new UserSkillKey(resourceUserId, skillId);
        var userSkill = await _userSkillRepository.GetById(key, cancellationToken);

        if (userSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.ResourceSkillNotFound);
        }

        _mapper.Map(request, userSkill);
        _userSkillRepository.Update(userSkill);
        await _userSkillRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task Remove(int resourceUserId, int skillId, CancellationToken cancellationToken = default)
    {
        var key = new UserSkillKey(resourceUserId, skillId);
        var userSkill = await _userSkillRepository.GetById(key, cancellationToken);

        if (userSkill is null)
        {
            throw new KeyNotFoundException(AppConstants.Skills.ResourceSkillNotFound);
        }

        _userSkillRepository.Remove(userSkill);
        await _userSkillRepository.SaveChanges(cancellationToken);
    }

    private async Task<Skill> GetOrCreateSkill(AddResourceSkillRequest request, CancellationToken cancellationToken)
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

    private async Task<User> GetResourceUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetById(userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException(AppConstants.Resources.NotFound);
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
