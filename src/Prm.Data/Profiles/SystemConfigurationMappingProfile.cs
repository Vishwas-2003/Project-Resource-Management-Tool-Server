using AutoMapper;
using Prm.Common.Models.SystemConfigurations;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class SystemConfigurationMappingProfile : Profile
{
    public SystemConfigurationMappingProfile()
    {
        CreateMap<SystemConfiguration, SystemConfigurationResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ConfigurationType, o => o.MapFrom(s => s.ConfigurationType))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value));
    }
}
