using AutoMapper;
using Prm.Common.Constants;
using Prm.Common.Models.Users;
using Prm.Data.Entities;

namespace Prm.Data.Profiles;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserSummary>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.Name.ToUpperInvariant()))
            .ForMember(
                d => d.Status,
                o => o.MapFrom(s => s.IsActive ? UserConstants.StatusActive : UserConstants.StatusInactive));
    }
}
