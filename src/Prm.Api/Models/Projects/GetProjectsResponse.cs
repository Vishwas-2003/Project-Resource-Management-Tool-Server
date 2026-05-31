namespace Prm.Api.Models.Projects;

public class GetProjectsResponse
{
    public IReadOnlyList<ProjectListItemResponse> Projects { get; set; } = [];
}
