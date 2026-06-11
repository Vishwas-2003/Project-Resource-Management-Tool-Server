using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Manager;
using Prm.Common.Models.Projects;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class ProjectService(
    IProjectRepository _projectRepository,
    IProjectRiskFlagRepository _projectRiskFlagRepository,
    IUserRepository _userRepository,
    IMapper _mapper) : IProjectService
{
    public async Task<int> Add(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDateRange(request.StartDate, request.EndDate);
        await ValidateManager(request.ManagerUserId, cancellationToken);

        var name = request.Name.Trim();
        if (await _projectRepository.ExistsByName(name, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Projects.NameExists);
        }

        var project = _mapper.Map<Project>(request);
        project.Name = name;
        project.Status = MapProjectStatus(request.Status);
        project.ManagerUserId = request.ManagerUserId;
        project.TotalStoryPoints = request.TotalStoryPoints;
        project.HealthStatus = ManagerConstants.HealthOnTrack;

        await _projectRepository.Add(project, cancellationToken);
        await _projectRepository.SaveChanges(cancellationToken);

        return project.Id;
    }

    public async Task<ProjectListResult> GetProjects(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllWithManager(cancellationToken);
        var summaries = _mapper.Map<List<ProjectSummary>>(projects);
        for (var rowIndex = 0; rowIndex < summaries.Count; rowIndex++)
        {
            summaries[rowIndex].RowNumber = rowIndex + 1;
        }

        var projectsById = projects.ToDictionary(x => x.Id);
        foreach (var summary in summaries)
        {
            var project = projectsById[summary.Id];
            summary.TotalStoryPoints = project.TotalStoryPoints;
            summary.StoryPointsDone = project.Milestones
                .Where(m => m.Status == MilestoneConstants.StatusDone)
                .Sum(m => m.StoryPoints);
        }

        return new ProjectListResult
        {
            Projects = summaries,
        };
    }

    public async Task<bool> Update(
        int projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(request.StartDate, request.EndDate);
        await ValidateManager(request.ManagerUserId, cancellationToken);

        var project = await GetProjectOrThrow(projectId, cancellationToken);

        var name = request.Name.Trim();
        if (await _projectRepository.ExistsByName(name, projectId, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Projects.NameExists);
        }

        _mapper.Map(request, project);
        project.Name = name;
        project.Status = MapProjectStatus(request.Status);
        project.ManagerUserId = request.ManagerUserId;
        project.TotalStoryPoints = request.TotalStoryPoints;

        _projectRepository.Update(project);
        await _projectRepository.SaveChanges(cancellationToken);

        return true;
    }

    public async Task<ManagerProjectListResult> GetMyProjects(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureManagerUserOrThrow(managerUserId, cancellationToken);
        var projects = await _projectRepository.GetByManagerUserId(managerUserId, cancellationToken);

        var summaries = new List<ManagerProjectSummary>();
        var rowNumber = 0;
        foreach (var project in projects)
        {
            rowNumber++;
            summaries.Add(new ManagerProjectSummary
            {
                RowNumber = rowNumber,
                Id = project.Id,
                Name = project.Name,
                EndDate = project.EndDate,
                HealthStatus = project.HealthStatus,
            });
        }

        return new ManagerProjectListResult { Projects = summaries };
    }

    public async Task<ManagerProjectDetailResponse> GetProjectDetail(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        await EnsureManagerUserOrThrow(managerUserId, cancellationToken);
        var project = await _projectRepository.GetByIdWithDetails(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        if (project.ManagerUserId != managerUserId)
        {
            throw new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var storedRiskFlags = await _projectRiskFlagRepository.GetByProjectId(projectId, cancellationToken);
        var activeAllocations = project.Allocations
            .Where(allocation => allocation.FromDate <= today && allocation.ToDate >= today)
            .OrderBy(allocation => allocation.User.FullName)
            .ToList();

        return new ManagerProjectDetailResponse
        {
            Id = project.Id,
            Name = project.Name,
            HealthStatus = project.HealthStatus,
            RiskFlags = storedRiskFlags
                .Select(flag => new RiskFlagItem
                {
                    Outcome = flag.Outcome,
                    Message = flag.Message,
                })
                .ToList(),
            Milestones = project.Milestones
                .OrderBy(milestone => milestone.DueDate)
                .ThenBy(milestone => milestone.Id)
                .Select((milestone, rowIndex) => new ManagerMilestoneRow
                {
                    RowNumber = rowIndex + 1,
                    Title = milestone.Title,
                    DueDate = milestone.DueDate,
                    Status = milestone.Status,
                    IsOverdue = IsMilestoneOverdue(milestone, today),
                })
                .ToList(),
            AllocatedResources = activeAllocations
                .Select(allocation => new ProjectResourceRow
                {
                    Name = allocation.User.FullName,
                    UtilizationPercent = allocation.UtilizationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate,
                })
                .ToList(),
        };
    }

    private async Task EnsureManagerUserOrThrow(int userId, CancellationToken cancellationToken)
    {
        var manager = await _userRepository.GetActiveManagerById(userId, cancellationToken);
        if (manager is null)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }
    }

    private static bool IsMilestoneOverdue(Milestone milestone, DateOnly today) =>
        milestone.Status != MilestoneConstants.StatusDone && milestone.DueDate < today;

    private async Task<Project> GetProjectOrThrow(int projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithManager(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        return project;
    }

    private async Task ValidateManager(int managerUserId, CancellationToken cancellationToken)
    {
        var manager = await _userRepository.GetActiveManagerById(managerUserId, cancellationToken);
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
            (int)ProjectStatusEnum.Completed => ProjectConstants.StatusCompleted,
            _ => throw new ArgumentException(AppConstants.Projects.InvalidStatus),
        };
    }
}
