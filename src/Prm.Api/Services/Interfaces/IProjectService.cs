using Prm.Common.Models.Manager;
using Prm.Common.Models.Projects;

namespace Prm.Api.Services.Interfaces;

public interface IProjectService
{
    Task<int> Add(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectListResult> GetProjects(CancellationToken cancellationToken = default);
    Task<bool> Update(int projectId, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ManagerProjectListResult> GetMyProjects(int managerUserId, CancellationToken cancellationToken = default);
    Task<ManagerProjectDetailResponse> GetProjectDetail(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default);
}
