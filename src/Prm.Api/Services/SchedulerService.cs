using Prm.Api.Services.Interfaces;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;
using Prm.Data.Repositories.Models;

namespace Prm.Api.Services;

public partial class SchedulerService(
    IAllocationRepository _allocationRepository,
    IUserRepository _userRepository,
    IProjectRepository _projectRepository,
    IProjectRiskFlagRepository _projectRiskFlagRepository,
    ITimesheetRepository _timesheetRepository,
    IProjectHealthService _projectHealthService,
    ILogger<SchedulerService> _logger) : ISchedulerService
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduler job started.");

        await UpdateResourcesStatus(cancellationToken);
        await RecordMissedTimesheets(cancellationToken);
        await ComputeProjectHealth(cancellationToken);

        _logger.LogInformation("Scheduler job completed.");
    }

    private async Task UpdateResourcesStatus(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetResourceUsers(
            new ResourceFilter { IncludeInactive = false },
            cancellationToken);

        foreach (var user in users)
        {
            await ApplyStatus(user, cancellationToken);
        }

        await _userRepository.SaveChanges(cancellationToken);
        _logger.LogInformation("Updated bench status for {ResourceCount} resources.", users.Count);
    }

    private async Task ApplyStatus(
        User user,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utilization = await _allocationRepository.SumUtilizationForUserInPeriod(
            new UserAllocationPeriodQuery
            {
                UserId = user.Id,
                FromDate = today,
                ToDate = today,
            },
            cancellationToken);

        var status = utilization > 0
            ? ResourceStatusTypeEnum.Allocated
            : ResourceStatusTypeEnum.Bench;

        await _userRepository.SetCurrentResourceStatus(user.Id, (int)status, cancellationToken);
    }
}
