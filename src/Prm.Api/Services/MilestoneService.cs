using AutoMapper;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Milestones;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class MilestoneService(
    IProjectRepository _projectRepository,
    IMilestoneRepository _milestoneRepository,
    IMapper _mapper) : IMilestoneService
{
    public async Task<ProjectMilestonesResult> GetByProjectId(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrow(projectId, cancellationToken);
        var milestones = await _milestoneRepository.GetByProjectId(projectId, cancellationToken);

        var summaries = _mapper.Map<List<MilestoneSummary>>(milestones);
        for (var rowIndex = 0; rowIndex < summaries.Count; rowIndex++)
        {
            summaries[rowIndex].RowNumber = rowIndex + 1;
        }

        return new ProjectMilestonesResult
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Milestones = summaries,
            TotalStoryPoints = summaries.Sum(x => x.StoryPoints),
            CompletedStoryPoints = summaries
                .Where(x => x.Status == MilestoneConstants.StatusDone)
                .Sum(x => x.StoryPoints),
            RemainingStoryPoints = summaries
                .Where(x => x.Status != MilestoneConstants.StatusDone)
                .Sum(x => x.StoryPoints),
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

        ValidateStoryPointsWithinProject(request.StoryPoints, project);

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

        var project = await GetProjectOrThrow(projectId, cancellationToken);
        ValidateStoryPointsWithinProjectForUpdate(request.StoryPoints, project, milestoneId);

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

    private static void ValidateStoryPointsWithinProject(
        int milestoneStoryPoints,
        Project project)
    {
        var used = project.Milestones.Sum(m => m.StoryPoints);
        var newTotal = used + milestoneStoryPoints;
        if (newTotal > project.TotalStoryPoints)
        {
            throw new InvalidOperationException(AppConstants.Milestones.StoryPointsExceedProjectTotal);
        }
    }

    private static void ValidateStoryPointsWithinProjectForUpdate(
        int milestoneStoryPoints,
        Project project,
        int milestoneId)
    {
        var usedWithoutThis = project.Milestones
            .Where(m => m.Id != milestoneId)
            .Sum(m => m.StoryPoints);
        var newTotal = usedWithoutThis + milestoneStoryPoints;
        if (newTotal > project.TotalStoryPoints)
        {
            throw new InvalidOperationException(AppConstants.Milestones.StoryPointsExceedProjectTotal);
        }
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
