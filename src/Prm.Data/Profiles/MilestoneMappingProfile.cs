using AutoMapper;
using Prm.Common.Models.Milestones;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class MilestoneMappingProfile : Profile
{
    public MilestoneMappingProfile()
    {
        CreateMap<Milestone, MilestoneSummary>();

        CreateMap<AddMilestoneRequest, Milestone>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ProjectId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Project, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore())
            .ForMember(d => d.Title, o => o.MapFrom(s => s.Title.Trim()));

        CreateMap<UpdateMilestoneRequest, Milestone>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ProjectId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Project, o => o.Ignore())
            .ForMember(d => d.CreatedAtUtc, o => o.Ignore())
            .ForMember(d => d.ModifiedAtUtc, o => o.Ignore())
            .ForMember(d => d.CreatedByUserId, o => o.Ignore())
            .ForMember(d => d.ModifiedByUserId, o => o.Ignore())
            .ForMember(d => d.CreatedByUser, o => o.Ignore())
            .ForMember(d => d.ModifiedByUser, o => o.Ignore())
            .ForMember(d => d.Title, o => o.MapFrom(s => s.Title.Trim()));
    }
}
