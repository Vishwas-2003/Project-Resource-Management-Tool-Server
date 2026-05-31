using AutoMapper;
using Prm.Api.Models.SystemConfigurations;
using Prm.Data.Entities;

namespace Prm.Api.Profiles;

public class SystemConfigurationProfile : Profile
{
    public SystemConfigurationProfile()
    {
        CreateMap<SystemConfiguration, SystemConfigurationResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ConfigurationType, o => o.MapFrom(s => s.ConfigurationType))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value));
    }
}
