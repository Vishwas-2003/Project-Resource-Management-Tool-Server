using Prm.Api.Models.Email;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class TimesheetReminderService(
    IUserRepository _userRepository,
    IAllocationRepository _allocationRepository,
    ITimesheetRepository _timesheetRepository,
    IEmailNotificationHistoryRepository _emailNotificationHistoryRepository,
    IEmailNotificationService _emailNotificationService,
    TimeProvider _timeProvider,
    ILogger<TimesheetReminderService> _logger) : ITimesheetReminderService
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        var dayOfWeek = utcNow.DayOfWeek;

        if (dayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday))
        {
            _logger.LogInformation(
                "Timesheet reminder job skipped because today ({DayOfWeek}) is outside Mon-Wed.",
                dayOfWeek);
            return;
        }

        _logger.LogInformation("Timesheet reminder job started for {DayOfWeek}.", dayOfWeek);

        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var missingResources = await GetMissingTimesheetResources(lastCompletedWeekStart, cancellationToken);
        var processed = 0;
        var emailsSent = 0;
        var skippedAlreadySent = 0;

        foreach (var resource in missingResources)
        {
            try
            {
                var result = await ProcessResourceAsync(
                    resource,
                    lastCompletedWeekStart,
                    today,
                    dayOfWeek,
                    cancellationToken);
                processed++;
                emailsSent += result.EmailsSent;
                skippedAlreadySent += result.SkippedAlreadySent;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process timesheet reminder for resource {ResourceUserId}.",
                    resource.Id);
            }
        }

        _logger.LogInformation(
            "Timesheet reminder job completed for {DayOfWeek}. Processed {ProcessedCount} of {MissingCount} resources. Sent {EmailsSent}, skipped {SkippedAlreadySent}.",
            dayOfWeek,
            processed,
            missingResources.Count,
            emailsSent,
            skippedAlreadySent);
    }

    private async Task<(int EmailsSent, int SkippedAlreadySent)> ProcessResourceAsync(
        User resource,
        DateOnly weekStart,
        DateOnly sentOnDate,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Monday:
            {
                var timesheet = await _timesheetRepository.GetOrEnsureMissedTimesheetAsync(
                    resource.Id,
                    weekStart,
                    cancellationToken);
                await _timesheetRepository.SaveChanges(cancellationToken);
                return await SendMissedTimesheetEmailAsync(
                    resource,
                    timesheet.Id,
                    TimesheetReminderEmailBuilder.BuildMondayReminder(resource, weekStart),
                    sentOnDate,
                    cancellationToken);
            }
            case DayOfWeek.Tuesday:
            {
                var timesheet = await _timesheetRepository.GetOrEnsureMissedTimesheetAsync(
                    resource.Id,
                    weekStart,
                    cancellationToken);
                await _timesheetRepository.SaveChanges(cancellationToken);
                return await SendMissedTimesheetEmailAsync(
                    resource,
                    timesheet.Id,
                    TimesheetReminderEmailBuilder.BuildTuesdayWarning(resource, weekStart),
                    sentOnDate,
                    cancellationToken);
            }
            case DayOfWeek.Wednesday:
                return await BlockAndNotifyAsync(resource, weekStart, sentOnDate, cancellationToken);
            default:
                return (0, 0);
        }
    }

    private async Task<(int EmailsSent, int SkippedAlreadySent)> BlockAndNotifyAsync(
        User resource,
        DateOnly weekStart,
        DateOnly sentOnDate,
        CancellationToken cancellationToken)
    {
        var existing = await _timesheetRepository.GetByUserAndWeek(resource.Id, weekStart, cancellationToken);
        if (existing?.Access == TimesheetConstants.AccessBlocked)
        {
            _logger.LogInformation(
                "Skipping Wednesday block for resource {ResourceUserId} because access is already blocked for {WeekStart}.",
                resource.Id,
                weekStart);
            return (0, 0);
        }

        var timesheet = await _timesheetRepository.EnsureBlockedTimesheetAsync(resource.Id, weekStart, cancellationToken);
        await _timesheetRepository.SaveChanges(cancellationToken);

        var resourceResult = await SendMissedTimesheetEmailAsync(
            resource,
            timesheet.Id,
            TimesheetReminderEmailBuilder.BuildWednesdayResourceBlocked(resource, weekStart),
            sentOnDate,
            cancellationToken);

        var manager = await _userRepository.GetCurrentManagerForResourceUserId(resource.Id, cancellationToken);
        if (manager is not { IsActive: true } || string.IsNullOrWhiteSpace(manager.Email))
        {
            _logger.LogWarning(
                "No active manager email found for resource {ResourceUserId} during Wednesday timesheet block.",
                resource.Id);
            return resourceResult;
        }

        var managerResult = await SendMissedTimesheetEmailAsync(
            manager,
            timesheet.Id,
            TimesheetReminderEmailBuilder.BuildWednesdayManagerBlocked(manager, resource, weekStart),
            sentOnDate,
            cancellationToken);

        return (
            resourceResult.EmailsSent + managerResult.EmailsSent,
            resourceResult.SkippedAlreadySent + managerResult.SkippedAlreadySent);
    }

    private async Task<(int EmailsSent, int SkippedAlreadySent)> SendMissedTimesheetEmailAsync(
        User recipient,
        int timesheetEntityId,
        EmailMessage email,
        DateOnly sentOnDate,
        CancellationToken cancellationToken)
    {
        if (await _emailNotificationHistoryRepository.ExistsForMissedTimesheetOnDateAsync(
                recipient.Id,
                sentOnDate,
                cancellationToken))
        {
            _logger.LogInformation(
                "Skipping missed timesheet email for user {UserId} because an email was already sent on {SentOnDate}.",
                recipient.Id,
                sentOnDate);
            return (0, 1);
        }

        await _emailNotificationService.SendAsync(email, cancellationToken);

        var sentAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _emailNotificationHistoryRepository.Add(
            new EmailNotificationHistory
            {
                EmailTypeId = (int)EmailNotificationTypeEnum.MissedTimeSheet,
                UserId = recipient.Id,
                EntityId = timesheetEntityId,
                SentOnDate = sentOnDate,
                SentAtUtc = sentAtUtc,
                RecipientEmail = email.ToEmail,
                Subject = email.Subject,
            },
            cancellationToken);
        await _emailNotificationHistoryRepository.SaveChanges(cancellationToken);

        _logger.LogInformation(
            "Logged missed timesheet email history for user {UserId}, timesheet {TimesheetId}, sent on {SentOnDate}.",
            recipient.Id,
            timesheetEntityId,
            sentOnDate);

        return (1, 0);
    }

    private async Task<IReadOnlyList<User>> GetMissingTimesheetResources(
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var resources = await _userRepository.GetResourceUsers(
            new ResourceFilter { IncludeInactive = false },
            cancellationToken);
        var missing = new List<User>();

        foreach (var resource in resources)
        {
            if (!UserAvailabilityHelper.IsWeekEligibleForUser(resource, weekStart))
            {
                continue;
            }

            var allocations = await _allocationRepository.GetOverlappingForUser(
                new Prm.Data.Repositories.Models.UserAllocationPeriodQuery
                {
                    UserId = resource.Id,
                    FromDate = weekStart,
                    ToDate = weekEnd,
                },
                cancellationToken);

            if (allocations.Count == 0)
            {
                continue;
            }

            if (await _timesheetRepository.IsSubmittedForUserWeek(resource.Id, weekStart, cancellationToken))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(resource.Email))
            {
                _logger.LogWarning(
                    "Skipping timesheet reminder for resource {ResourceUserId} because email is missing.",
                    resource.Id);
                continue;
            }

            missing.Add(resource);
        }

        return missing;
    }
}
