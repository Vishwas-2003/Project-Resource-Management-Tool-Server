using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Projects;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    IMapper mapper) : IProjectService
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<int> Add(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(request.StartDate, request.EndDate);
        await ValidateManager(request.ManagerEmployeeId, cancellationToken);

        var name = request.Name.Trim();
        if (await _projectRepository.ExistsByName(name, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Projects.NameExists);
        }

        var project = _mapper.Map<Project>(request);
        project.Name = name;
        project.Status = MapProjectStatus(request.Status);
        project.ManagerEmployeeId = request.ManagerEmployeeId;

        await _projectRepository.Add(project, cancellationToken);
        await _projectRepository.SaveChanges(cancellationToken);

        return project.Id;
    }

    public async Task<ProjectListResult> GetProjects(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllWithManager(cancellationToken);

        return new ProjectListResult
        {
            Projects = _mapper.Map<IReadOnlyList<ProjectSummary>>(projects),
        };
    }

    public async Task<bool> Update(
        int projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(request.StartDate, request.EndDate);
        await ValidateManager(request.ManagerEmployeeId, cancellationToken);

        var project = await GetProjectOrThrow(projectId, cancellationToken);

        var name = request.Name.Trim();
        if (await _projectRepository.ExistsByName(name, projectId, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Projects.NameExists);
        }

        _mapper.Map(request, project);
        project.Name = name;
        project.Status = MapProjectStatus(request.Status);
        project.ManagerEmployeeId = request.ManagerEmployeeId;

        _projectRepository.Update(project);
        await _projectRepository.SaveChanges(cancellationToken);

        return true;
    }

    private async Task<Project> GetProjectOrThrow(int projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithManager(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        return project;
    }

    private async Task ValidateManager(int managerEmployeeId, CancellationToken cancellationToken)
    {
        var manager = await _employeeRepository.GetManagerById(managerEmployeeId, cancellationToken);
        if (manager is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.ManagerNotFound);
        }
    }

    private static void ValidateDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException(AppConstants.Projects.InvalidDateRange);
        }
    }

    private static string MapProjectStatus(int status)
    {
        return status switch
        {
            (int)ProjectStatusEnum.Planned => ProjectConstants.StatusPlanned,
            (int)ProjectStatusEnum.Active => ProjectConstants.StatusActive,
            (int)ProjectStatusEnum.OnHold => ProjectConstants.StatusOnHold,
            _ => throw new ArgumentException(AppConstants.Projects.InvalidStatus),
        };
    }
}
