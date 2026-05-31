using AutoMapper;
using Prm.Api.Models.Users;
using Prm.Common.Models.Users;

namespace Prm.Api.Profiles;

public class UserApiProfile : Profile
{
    public UserApiProfile()
    {
        CreateMap<UserSummary, UserListItemResponse>();
        CreateMap<UserListResult, GetUsersResponse>();
    }
}
