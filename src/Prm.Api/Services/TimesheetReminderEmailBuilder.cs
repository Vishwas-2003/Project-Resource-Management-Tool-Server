using Prm.Api.Models.Email;
using Prm.Common.Constants;
using Prm.Data.Entities;

namespace Prm.Api.Services;

internal static class TimesheetReminderEmailBuilder
{
    public static EmailMessage BuildMondayReminder(User resource, DateOnly weekStart) =>
        BuildResourceEmail(
            resource,
            string.Format(AppConstants.Email.TimesheetReminderMondaySubject, weekStart),
            AppConstants.Email.TimesheetMondayTitle,
            EmailLayoutBuilder.AccentInfo,
            BuildMondayBody(resource, weekStart),
            BuildMondayText(resource, weekStart));

    public static EmailMessage BuildTuesdayWarning(User resource, DateOnly weekStart) =>
        BuildResourceEmail(
            resource,
            string.Format(AppConstants.Email.TimesheetReminderTuesdaySubject, weekStart),
            AppConstants.Email.TimesheetTuesdayTitle,
            EmailLayoutBuilder.AccentWarning,
            BuildTuesdayBody(resource, weekStart),
            BuildTuesdayText(resource, weekStart));

    public static EmailMessage BuildWednesdayResourceBlocked(User resource, DateOnly weekStart) =>
        BuildResourceEmail(
            resource,
            string.Format(AppConstants.Email.TimesheetBlockedResourceSubject, weekStart),
            AppConstants.Email.TimesheetBlockedResourceTitle,
            EmailLayoutBuilder.AccentDanger,
            BuildWednesdayResourceBody(resource, weekStart),
            BuildWednesdayResourceText(resource, weekStart));

    public static EmailMessage BuildWednesdayManagerBlocked(
        User manager,
        User resource,
        DateOnly weekStart)
    {
        var subject = string.Format(
            AppConstants.Email.TimesheetBlockedManagerSubject,
            resource.FullName,
            weekStart);

        return new EmailMessage
        {
            ToEmail = manager.Email,
            ToName = manager.FullName,
            Subject = subject,
            HtmlBody = EmailLayoutBuilder.BuildHtml(
                AppConstants.Email.TimesheetBlockedManagerTitle,
                EmailLayoutBuilder.AccentDanger,
                BuildWednesdayManagerBody(manager, resource, weekStart)),
            TextBody = EmailLayoutBuilder.BuildText(BuildWednesdayManagerText(manager, resource, weekStart)),
        };
    }

    private static EmailMessage BuildResourceEmail(
        User resource,
        string subject,
        string title,
        string accentColor,
        string bodyHtml,
        string textBody) =>
        new()
        {
            ToEmail = resource.Email,
            ToName = resource.FullName,
            Subject = subject,
            HtmlBody = EmailLayoutBuilder.BuildHtml(title, accentColor, bodyHtml),
            TextBody = EmailLayoutBuilder.BuildText(textBody),
        };

    private static string BuildMondayBody(User resource, DateOnly weekStart)
    {
        var weekRange = FormatWeekRange(weekStart);
        return string.Concat(
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.Greeting, EmailLayoutBuilder.Encode(resource.FullName))),
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.TimesheetMondayBody, weekRange)),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetMondayCallToAction));
    }

    private static string BuildTuesdayBody(User resource, DateOnly weekStart)
    {
        var weekRange = FormatWeekRange(weekStart);
        return string.Concat(
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.Greeting, EmailLayoutBuilder.Encode(resource.FullName))),
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.TimesheetTuesdayBody, weekRange)),
            EmailLayoutBuilder.InfoPanel(
                EmailLayoutBuilder.AccentWarning,
                AppConstants.Email.TimesheetTuesdayWarning),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetTuesdayCallToAction));
    }

    private static string BuildWednesdayResourceBody(User resource, DateOnly weekStart)
    {
        var weekRange = FormatWeekRange(weekStart);
        return string.Concat(
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.Greeting, EmailLayoutBuilder.Encode(resource.FullName))),
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.TimesheetBlockedResourceBody, weekRange)),
            EmailLayoutBuilder.InfoPanel(
                EmailLayoutBuilder.AccentDanger,
                AppConstants.Email.TimesheetBlockedResourceNotice),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetBlockedResourceReason),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetBlockedResourceAction));
    }

    private static string BuildWednesdayManagerBody(User manager, User resource, DateOnly weekStart)
    {
        var weekRange = FormatWeekRange(weekStart);
        return string.Concat(
            EmailLayoutBuilder.Paragraph(string.Format(AppConstants.Email.Greeting, EmailLayoutBuilder.Encode(manager.FullName))),
            EmailLayoutBuilder.Paragraph(string.Format(
                AppConstants.Email.TimesheetBlockedManagerBody,
                EmailLayoutBuilder.Encode(resource.FullName),
                weekRange)),
            EmailLayoutBuilder.InfoPanel(
                EmailLayoutBuilder.AccentDanger,
                AppConstants.Email.TimesheetBlockedManagerNotice),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetBlockedManagerReason),
            EmailLayoutBuilder.Paragraph(AppConstants.Email.TimesheetBlockedManagerAction));
    }

    private static string BuildMondayText(User resource, DateOnly weekStart) =>
        string.Concat(
            string.Format(AppConstants.Email.Greeting, resource.FullName),
            Environment.NewLine,
            Environment.NewLine,
            string.Format(AppConstants.Email.TimesheetMondayTextBody, FormatWeekRangeText(weekStart)));

    private static string BuildTuesdayText(User resource, DateOnly weekStart) =>
        string.Concat(
            string.Format(AppConstants.Email.Greeting, resource.FullName),
            Environment.NewLine,
            Environment.NewLine,
            string.Format(AppConstants.Email.TimesheetTuesdayTextBody, FormatWeekRangeText(weekStart)));

    private static string BuildWednesdayResourceText(User resource, DateOnly weekStart) =>
        string.Concat(
            string.Format(AppConstants.Email.Greeting, resource.FullName),
            Environment.NewLine,
            Environment.NewLine,
            string.Format(AppConstants.Email.TimesheetBlockedResourceTextBody, FormatWeekRangeText(weekStart)));

    private static string BuildWednesdayManagerText(User manager, User resource, DateOnly weekStart) =>
        string.Concat(
            string.Format(AppConstants.Email.Greeting, manager.FullName),
            Environment.NewLine,
            Environment.NewLine,
            string.Format(
                AppConstants.Email.TimesheetBlockedManagerTextBody,
                resource.FullName,
                FormatWeekRangeText(weekStart)));

    private static string FormatWeekRange(DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        return $"<strong>{weekStart:yyyy-MM-dd}</strong> to <strong>{weekEnd:yyyy-MM-dd}</strong>";
    }

    private static string FormatWeekRangeText(DateOnly weekStart)
    {
        var weekEnd = TimesheetWeekHelper.GetWeekEnd(weekStart);
        return string.Format(AppConstants.Email.WeekRange, weekStart, weekEnd);
    }
}
