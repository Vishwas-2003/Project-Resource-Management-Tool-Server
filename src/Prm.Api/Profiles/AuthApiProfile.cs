using AutoMapper;
using Prm.Api.Models.Auth;
using Prm.Common.Models.Auth;

namespace Prm.Api.Profiles;

public class AuthApiProfile : Profile
{
    public AuthApiProfile()
    {
        CreateMap<AuthResponse, LoginResponse>();
        CreateMap<AuthenticatedUser, UserResponse>();
        CreateMap<AuthTokens, TokenResponse>();
    }
}
