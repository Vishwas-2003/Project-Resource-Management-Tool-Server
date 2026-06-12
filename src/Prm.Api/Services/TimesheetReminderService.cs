using Prm.Api.Models.Email;
using Prm.Api.Services.Interfaces;
using Prm.Common.Constants;
using Prm.Common.Enums;
using Prm.Common.Models.Resources;
using Prm.Data.Entities;
using Prm.Data.Repositories.Interfaces;

namespace Prm.Api.Services;

public class TimesheetReminderService(
    IUserRepository userRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    IEmailNotificationService emailNotificationService,
    TimeProvider timeProvider,
    ILogger<TimesheetReminderService> logger) : ITimesheetReminderService
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);
        var dayOfWeek = utcNow.DayOfWeek;

        if (dayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday))
        {
            logger.LogInformation(
                "Timesheet reminder job skipped because today ({DayOfWeek}) is outside Mon-Wed.",
                dayOfWeek);
            return;
        }

        logger.LogInformation("Timesheet reminder job started for {DayOfWeek}.", dayOfWeek);

        var lastCompletedWeekStart = TimesheetWeekHelper.GetLastCompletedWeekStart(today);
        var missingResources = await GetMissingTimesheetResources(lastCompletedWeekStart, cancellationToken);
        var processed = 0;

        foreach (var resource in missingResources)
        {
            try
            {
                await ProcessResourceAsync(resource, lastCompletedWeekStart, dayOfWeek, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process timesheet reminder for resource {ResourceUserId}.",
                    resource.Id);
            }
        }

        logger.LogInformation(
            "Timesheet reminder job completed for {DayOfWeek}. Processed {ProcessedCount} of {MissingCount} resources.",
            dayOfWeek,
            processed,
            missingResources.Count);
    }

    private async Task ProcessResourceAsync(
        User resource,
        DateOnly weekStart,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken)
    {
        switch (dayOfWeek)
        {
            case DayOfWeek.Monday:
                await SendEmailAsync(
                    TimesheetReminderEmailBuilder.BuildMondayReminder(resource, weekStart),
                    cancellationToken);
                break;
            case DayOfWeek.Tuesday:
                await SendEmailAsync(
                    TimesheetReminderEmailBuilder.BuildTuesdayWarning(resource, weekStart),
                    cancellationToken);
                break;
            case DayOfWeek.Wednesday:
                await BlockAndNotifyAsync(resource, weekStart, cancellationToken);
                break;
        }
    }

    private async Task BlockAndNotifyAsync(
        User resource,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var existing = await timesheetRepository.GetByUserAndWeek(resource.Id, weekStart, cancellationToken);
        if (existing?.Access == TimesheetConstants.AccessBlocked)
        {
            logger.LogInformation(
                "Skipping Wednesday block for resource {ResourceUserId} because access is already blocked for {WeekStart}.",
                resource.Id,
                weekStart);
            return;
        }

        await timesheetRepository.EnsureBlockedTimesheetAsync(resource.Id, weekStart, cancellationToken);
        await timesheetRepository.SaveChanges(cancellationToken);

        await SendEmailAsync(
            TimesheetReminderEmailBuilder.BuildWednesdayResourceBlocked(resource, weekStart),
            cancellationToken);

        var manager = await userRepository.GetCurrentManagerForResourceUserId(resource.Id, cancellationToken);
        if (manager is { IsActive: true } && !string.IsNullOrWhiteSpace(manager.Email))
        {
            await SendEmailAsync(
                TimesheetReminderEmailBuilder.BuildWednesdayManagerBlocked(manager, resource, weekStart),
                cancellationToken);
        }
        else
        {
            logger.LogWarning(
                "No active manager email found for resource {ResourceUserId} during Wednesday timesheet block.",
                resource.Id);
        }
    }

    private async Task SendEmailAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        await emailNotificationService.SendAsync(email, cancellationToken);
    }

    private async Task<IReadOnlyList<User>> GetMissingTimesheetResources(
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var resources = await userRepository.GetResourceUsers(
            new ResourceFilter { IncludeInactive = false },
            cancellationToken);
        var missing = new List<User>();

        foreach (var resource in resources)
        {
            if (!UserAvailabilityHelper.IsWeekEligibleForUser(resource, weekStart))
            {
                continue;
            }

            var allocations = await allocationRepository.GetOverlappingForUser(
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

            if (await timesheetRepository.IsSubmittedForUserWeek(resource.Id, weekStart, cancellationToken))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(resource.Email))
            {
                logger.LogWarning(
                    "Skipping timesheet reminder for resource {ResourceUserId} because email is missing.",
                    resource.Id);
                continue;
            }

            missing.Add(resource);
        }

        return missing;
    }
}
