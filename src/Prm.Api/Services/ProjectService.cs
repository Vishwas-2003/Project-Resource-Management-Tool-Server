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
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigurationRepository systemConfigurationRepository,
    IMapper mapper) : IProjectService
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly ITimesheetRepository _timesheetRepository = timesheetRepository;
    private readonly ISystemConfigurationRepository _systemConfigurationRepository = systemConfigurationRepository;
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

    public async Task<ManagerProjectListResult> GetMyProjects(
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var manager = await GetManagerEmployeeOrThrow(managerUserId, cancellationToken);
        var projects = await _projectRepository.GetByManagerEmployeeId(manager.Id, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxWeeklyHours = await GetMaxWeeklyHours(cancellationToken);

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
                HealthStatus = await ComputeHealthStatus(project, today, maxWeeklyHours, cancellationToken),
            });
        }

        return new ManagerProjectListResult { Projects = summaries };
    }

    public async Task<ManagerProjectDetailResponse> GetProjectDetail(
        int projectId,
        int managerUserId,
        CancellationToken cancellationToken = default)
    {
        var manager = await GetManagerEmployeeOrThrow(managerUserId, cancellationToken);
        var project = await _projectRepository.GetByIdWithDetails(projectId, cancellationToken);
        if (project is null)
        {
            throw new KeyNotFoundException(AppConstants.Projects.NotFound);
        }

        if (project.ManagerEmployeeId != manager.Id)
        {
            throw new UnauthorizedAccessException(AppConstants.Manager.ProjectNotOwned);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxWeeklyHours = await GetMaxWeeklyHours(cancellationToken);
        var health = await ComputeHealthStatus(project, today, maxWeeklyHours, cancellationToken);
        var riskFlags = await BuildRiskFlags(project, today, maxWeeklyHours, cancellationToken);
        var activeAllocations = project.Allocations
            .Where(allocation => allocation.FromDate <= today && allocation.ToDate >= today)
            .OrderBy(allocation => allocation.Employee.User.FullName)
            .ToList();

        return new ManagerProjectDetailResponse
        {
            Id = project.Id,
            Name = project.Name,
            HealthStatus = health,
            RiskFlags = riskFlags,
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
                    Name = allocation.Employee.User.FullName,
                    UtilizationPercent = allocation.UtilizationPercent,
                    FromDate = allocation.FromDate,
                    ToDate = allocation.ToDate,
                })
                .ToList(),
        };
    }

    private async Task<Employee> GetManagerEmployeeOrThrow(int userId, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetEmployeeByUserId(userId, cancellationToken);
        if (employee is null || employee.User.RoleId != (int)RoleNameEnum.Manager)
        {
            throw new KeyNotFoundException(AppConstants.Manager.ProfileNotFound);
        }

        return employee;
    }

    private async Task<int> GetMaxWeeklyHours(CancellationToken cancellationToken)
    {
        var config = await _systemConfigurationRepository.GetById(
            (int)ConfigurationOptionEnum.MaxWeeklyHours,
            cancellationToken);

        if (config is null || !int.TryParse(config.Value, out var hours) || hours <= 0)
        {
            return ManagerConstants.DefaultMaxWeeklyHours;
        }

        return hours;
    }

    private async Task<string> ComputeHealthStatus(
        Project project,
        DateOnly today,
        int maxWeeklyHours,
        CancellationToken cancellationToken)
    {
        var riskFlags = await BuildRiskFlags(project, today, maxWeeklyHours, cancellationToken);
        var failures = riskFlags.Count(flag => flag.Outcome == ManagerConstants.RiskFlagFail);

        if (failures >= ManagerConstants.RiskFlagCountForProjectUnderRisk)
        {
            return ManagerConstants.HealthAtRisk;
        }

        if (failures == ManagerConstants.RiskFlagCountForProjectNeedAttention)
        {
            return ManagerConstants.HealthAttention;
        }

        var hasOverdue = project.Milestones.Any(milestone => IsMilestoneOverdue(milestone, today));
        if (hasOverdue)
        {
            return ManagerConstants.HealthAttention;
        }

        return ManagerConstants.HealthOnTrack;
    }

    private async Task<IReadOnlyList<RiskFlagItem>> BuildRiskFlags(
        Project project,
        DateOnly today,
        int maxWeeklyHours,
        CancellationToken cancellationToken)
    {
        var flags = new List<RiskFlagItem>();
        var overdueMilestone = project.Milestones
            .Where(milestone => IsMilestoneOverdue(milestone, today))
            .OrderBy(milestone => milestone.DueDate)
            .FirstOrDefault();

        if (overdueMilestone is not null)
        {
            var daysOverdue = today.DayNumber - overdueMilestone.DueDate.DayNumber;
            flags.Add(new RiskFlagItem
            {
                Outcome = ManagerConstants.RiskFlagFail,
                Message = $"{overdueMilestone.Title} milestone is {daysOverdue} days overdue",
            });
        }

        var lastWeekStart = GetWeekStart(today).AddDays(-7);
        var activeAllocations = project.Allocations
            .Where(allocation => allocation.FromDate <= today && allocation.ToDate >= today)
            .ToList();

        foreach (var allocation in activeAllocations)
        {
            var expectedHours = allocation.UtilizationPercent * maxWeeklyHours / 100;
            var actualHours = await _timesheetRepository.GetHoursWorkedForEmployeeOnProjectInWeek(
                allocation.EmployeeId,
                project.Id,
                lastWeekStart,
                cancellationToken);

            if (expectedHours > 0 && actualHours < expectedHours)
            {
                flags.Add(new RiskFlagItem
                {
                    Outcome = ManagerConstants.RiskFlagFail,
                    Message =
                        $"{allocation.Employee.User.FullName} logged only {actualHours} hrs last week (expected {expectedHours} hrs)",
                });
                break;
            }
        }

        var totalAllocation = activeAllocations.Sum(x => x.UtilizationPercent);
        var allocationOk = totalAllocation > AllocationConstants.MinTotalUtilizationPercent && totalAllocation <= AllocationConstants.MaxTotalUtilizationPercent;
        flags.Add(new RiskFlagItem
        {
            Outcome = allocationOk ? ManagerConstants.RiskFlagPass : ManagerConstants.RiskFlagFail,
            Message = allocationOk
                ? ManagerConstants.ResourcesCorrectlyAllocated
                : ManagerConstants.ProjectResourcesNeedAttention,
        });

        return flags;
    }

    private static bool IsMilestoneOverdue(Milestone milestone, DateOnly today) =>
        milestone.Status != MilestoneConstants.StatusDone && milestone.DueDate < today;

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-offset);
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
