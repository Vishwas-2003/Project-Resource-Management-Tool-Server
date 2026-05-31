using AutoMapper;
using Prm.Api.Models.Milestones;
using Prm.Common.Models.Milestones;

namespace Prm.Api.Profiles;

public class MilestoneApiProfile : Profile
{
    public MilestoneApiProfile()
    {
        CreateMap<MilestoneSummary, MilestoneListItemResponse>();
        CreateMap<ProjectMilestonesResult, GetProjectMilestonesResponse>();
    }
}
