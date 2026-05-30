using AutoMapper;
using Prm.Api.Models.Skills;
using Prm.Common.Models.Skills;

namespace Prm.Api.Profiles;

public class SkillApiProfile : Profile
{
    public SkillApiProfile()
    {
        CreateMap<EmployeeSkillItem, EmployeeSkillItemResponse>();
        CreateMap<EmployeeSkillsResult, EmployeeSkillsResponse>();
    }
}
