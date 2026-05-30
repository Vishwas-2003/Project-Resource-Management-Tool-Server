using AutoMapper;
using Prm.Common.Models.Skills;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class SkillMappingProfile : Profile
{
    public SkillMappingProfile()
    {
        CreateMap<EmployeeSkill, EmployeeSkillItem>()
            .ForMember(d => d.SkillName, o => o.MapFrom(s => s.Skill.Name))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Skill.Category));

        CreateMap<AddEmployeeSkillRequest, EmployeeSkill>()
            .ForMember(d => d.EmployeeId, o => o.Ignore())
            .ForMember(d => d.SkillId, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Skill, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore())
            .ForMember(d => d.Proficiency, o => o.MapFrom(s => s.Proficiency.Trim()));

        CreateMap<UpdateEmployeeSkillRequest, EmployeeSkill>()
            .ForMember(d => d.EmployeeId, o => o.Ignore())
            .ForMember(d => d.SkillId, o => o.Ignore())
            .ForMember(d => d.Employee, o => o.Ignore())
            .ForMember(d => d.Skill, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore())
            .ForMember(d => d.Proficiency, o => o.MapFrom(s => s.Proficiency.Trim()));
    }
}
