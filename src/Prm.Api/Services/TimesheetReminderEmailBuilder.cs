using System.Net;
using Prm.Api.Models.Email;
using Prm.Common.Constants;
using Prm.Data.Entities;

namespace Prm.Api.Services;

internal static class TimesheetReminderEmailBuilder
{
    public static EmailMessage BuildMondayReminder(User resource, DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var subject = string.Format(AppConstants.Email.TimesheetReminderMondaySubject, weekStart);
        var html = $"""
            <p>Hi {Encode(resource.FullName)},</p>
            <p>Our records show that your timesheet for the week <strong>{weekStart:yyyy-MM-dd}</strong> to <strong>{weekEnd:yyyy-MM-dd}</strong> has not been submitted yet.</p>
            <p>Please log in to PRM and submit your timesheet as soon as possible.</p>
            """;
        var text =
            $"Hi {resource.FullName},{Environment.NewLine}{Environment.NewLine}"
            + $"Your timesheet for {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd} is still missing. "
            + "Please submit it in PRM as soon as possible.";

        return Create(resource, subject, html, text);
    }

    public static EmailMessage BuildTuesdayWarning(User resource, DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var subject = string.Format(AppConstants.Email.TimesheetReminderTuesdaySubject, weekStart);
        var html = $"""
            <p>Hi {Encode(resource.FullName)},</p>
            <p>Your timesheet for the week <strong>{weekStart:yyyy-MM-dd}</strong> to <strong>{weekEnd:yyyy-MM-dd}</strong> is still missing.</p>
            <p><strong>Important:</strong> If it is not submitted by end of day Tuesday, your timesheet access for this week will be blocked on Wednesday.</p>
            <p>Please submit your timesheet in PRM immediately to avoid access restrictions.</p>
            """;
        var text =
            $"Hi {resource.FullName},{Environment.NewLine}{Environment.NewLine}"
            + $"Your timesheet for {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd} is still missing. "
            + "If it is not submitted by end of day Tuesday, your timesheet access for this week will be blocked on Wednesday.";

        return Create(resource, subject, html, text);
    }

    public static EmailMessage BuildWednesdayResourceBlocked(User resource, DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var subject = string.Format(AppConstants.Email.TimesheetBlockedResourceSubject, weekStart);
        var html = $"""
            <p>Hi {Encode(resource.FullName)},</p>
            <p>Your timesheet for the week <strong>{weekStart:yyyy-MM-dd}</strong> to <strong>{weekEnd:yyyy-MM-dd}</strong> was not submitted by the deadline.</p>
            <p><strong>Your timesheet access for this week is now blocked.</strong></p>
            <p>Reason: Missing timesheet submission after Monday and Tuesday reminders.</p>
            <p>Contact your manager to restore access. Once access is allowed, you may submit the timesheet for that week.</p>
            """;
        var text =
            $"Hi {resource.FullName},{Environment.NewLine}{Environment.NewLine}"
            + $"Your timesheet access for {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd} is blocked because the timesheet was not submitted. "
            + "Contact your manager to restore access.";

        return Create(resource, subject, html, text);
    }

    public static EmailMessage BuildWednesdayManagerBlocked(
        User manager,
        User resource,
        DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        var subject = string.Format(
            AppConstants.Email.TimesheetBlockedManagerSubject,
            resource.FullName,
            weekStart);
        var html = $"""
            <p>Hi {Encode(manager.FullName)},</p>
            <p><strong>{Encode(resource.FullName)}</strong> did not submit a timesheet for the week <strong>{weekStart:yyyy-MM-dd}</strong> to <strong>{weekEnd:yyyy-MM-dd}</strong>.</p>
            <p><strong>The employee's timesheet access for that week is now blocked.</strong></p>
            <p>Reason: Missing timesheet submission after automated Monday and Tuesday reminders.</p>
            <p>You can restore access in PRM when the employee is ready to submit the overdue timesheet.</p>
            """;
        var text =
            $"Hi {manager.FullName},{Environment.NewLine}{Environment.NewLine}"
            + $"{resource.FullName} did not submit a timesheet for {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}. "
            + "Their timesheet access for that week is blocked. Restore access in PRM when appropriate.";

        return new EmailMessage
        {
            ToEmail = manager.Email,
            ToName = manager.FullName,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
    }

    private static EmailMessage Create(User resource, string subject, string html, string text) =>
        new()
        {
            ToEmail = resource.Email,
            ToName = resource.FullName,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
