using Prm.Common.Constants;
using Prm.Common.Models.Timesheets;
using Prm.Data.Entities;

namespace Prm.Api.Services;

public partial class TimesheetService
{
    public async Task<RestoreTimesheetAccessResponse> AllowTimesheetAccess(
        int managerUserId,
        int resourceUserId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        if (!await _userRepository.IsResourceManagedByManager(resourceUserId, managerUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException(AppConstants.Timesheets.ResourceNotOnTeam);
        }

        var normalizedWeekStart = TimesheetWeekHelper.GetWeekStart(weekStart);
        var timesheet = await _timesheetRepository.GetByUserAndWeek(
            resourceUserId,
            normalizedWeekStart,
            cancellationToken)
            ?? throw new KeyNotFoundException(AppConstants.Timesheets.NotFound);

        if (timesheet.Access == TimesheetConstants.AccessAllowed)
        {
            throw new InvalidOperationException(AppConstants.Timesheets.AccessAlreadyAllowed);
        }

        if (timesheet.Access != TimesheetConstants.AccessBlocked)
        {
            throw new InvalidOperationException(AppConstants.Timesheets.AccessRestoreInvalidState);
        }

        if (timesheet.Status == TimesheetConstants.StatusSubmitted)
        {
            throw new InvalidOperationException(AppConstants.Timesheets.AlreadySubmitted);
        }

        timesheet.Access = TimesheetConstants.AccessAllowed;
        _timesheetRepository.Update(timesheet);
        await _timesheetRepository.SaveChanges(cancellationToken);

        return new RestoreTimesheetAccessResponse
        {
            ResourceUserId = resourceUserId,
            WeekStart = normalizedWeekStart,
            Access = TimesheetConstants.AccessAllowed,
            Message = AppConstants.Timesheets.AccessRestoredSuccessfully,
        };
    }

    private async Task EnsureTimesheetAccessAllowed(
        int userId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var timesheet = await _timesheetRepository.GetByUserAndWeek(userId, weekStart, cancellationToken);
        if (timesheet?.Access == TimesheetConstants.AccessBlocked)
        {
            throw new InvalidOperationException(AppConstants.Timesheets.AccessBlocked);
        }
    }
}
