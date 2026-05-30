using AutoMapper;
using Prm.Common.Models.Auth;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<User, AuthenticatedUser>()
            .ForMember(d => d.UserId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Role.Name));
    }
}
