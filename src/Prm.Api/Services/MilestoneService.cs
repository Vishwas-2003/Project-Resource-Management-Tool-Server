using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Milestones;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class MilestoneService(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IMapper mapper) : IMilestoneService
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly IMilestoneRepository _milestoneRepository = milestoneRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<ProjectMilestonesResult> GetByProjectId(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrow(projectId, cancellationToken);
        var milestones = await _milestoneRepository.GetByProjectId(projectId, cancellationToken);

        return new ProjectMilestonesResult
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Milestones = _mapper.Map<IReadOnlyList<MilestoneSummary>>(milestones),
        };
    }

    public async Task<int> Add(
        int projectId,
        AddMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrow(projectId, cancellationToken);

        var title = request.Title.Trim();
        if (await _milestoneRepository.ExistsByTitleForProject(projectId, title, cancellationToken))
        {
            throw new InvalidOperationException(AppConstants.Milestones.TitleExists);
        }

        ValidateDueDateWithinProject(request.DueDate, project);

        var milestone = _mapper.Map<Milestone>(request);
        milestone.ProjectId = projectId;
        milestone.Title = title;
        milestone.Status = MapMilestoneStatus(request.Status);

        await _milestoneRepository.Add(milestone, cancellationToken);
        await _milestoneRepository.SaveChanges(cancellationToken);

        return milestone.Id;
    }

    public async Task<bool> Update(
        int projectId,
        int milestoneId,
        UpdateMilestoneRequest request,
        CancellationToken cancellationToken = default)
    {
        await GetProjectOrThrow(projectId, cancellationToken);

        var milestone = await _milestoneRepository.GetByIdAndProjectId(milestoneId, projectId, cancellationToken);
        if (milestone is null)
        {
            throw new KeyNotFoundException(AppConstants.Milestones.NotFound);
        }

        _mapper.Map(request, milestone);
        milestone.Status = MapMilestoneStatus(request.Status);

        _milestoneRepository.Update(milestone);
        await _milestoneRepository.SaveChanges(cancellationToken);

        return true;
    }

    private async Task<Project> GetProjectOrThrow(int projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetById(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        return project;
    }

    private static void ValidateDueDateWithinProject(DateOnly dueDate, Project project)
    {
        if (dueDate < project.StartDate || dueDate > project.EndDate)
        {
            throw new ArgumentException(AppConstants.Milestones.InvalidDueDate);
        }
    }

    private static string MapMilestoneStatus(int status)
    {
        return status switch
        {
            (int)MilestoneStatusEnum.NotStarted => MilestoneConstants.StatusNotStarted,
            (int)MilestoneStatusEnum.InProgress => MilestoneConstants.StatusInProgress,
            (int)MilestoneStatusEnum.Done => MilestoneConstants.StatusDone,
            _ => throw new ArgumentException(AppConstants.Milestones.InvalidStatus),
        };
    }
}
