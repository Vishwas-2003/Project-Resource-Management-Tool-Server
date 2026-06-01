using AutoMapper;
using Prm.Data.Profiles;

namespace Prm.Api.Tests.Helpers;

internal static class MapperTestHelper
{
    internal static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EmployeeMappingProfile>();
            cfg.AddProfile<UserMappingProfile>();
            cfg.AddProfile<SkillMappingProfile>();
            cfg.AddProfile<ProjectMappingProfile>();
            cfg.AddProfile<MilestoneMappingProfile>();
            cfg.AddProfile<SystemConfigurationMappingProfile>();
        });

        return config.CreateMapper();
    }
}
