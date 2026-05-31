using AutoMapper;
using Prm.Api.Models.Projects;
using Prm.Common.Models.Projects;

namespace Prm.Api.Profiles;

public class ProjectApiProfile : Profile
{
    public ProjectApiProfile()
    {
        CreateMap<ProjectSummary, ProjectListItemResponse>();
        CreateMap<ProjectListResult, GetProjectsResponse>();
    }
}
