using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Configuration;

public class HangfireOptions
{
    public const string Section = "Hangfire";
    public string DashboardPath { get; set; } = "/hangfire";
    public string RecurringJobId { get; set; } = "prm-scheduler";
    public string ProjectRiskAlertRecurringJobId { get; set; } = "prm-project-risk-alert";
    public string TimesheetReminderRecurringJobId { get; set; } = "prm-timesheet-reminder";
    public string TimesheetReminderCron { get; set; } = "0 9 * * 1-3";
    public int DefaultSchedulerIntervalMinutes { get; set; } = 60;
    [Required]
    public string DashboardUsername { get; set; } = string.Empty;
    [Required]
    public string DashboardPassword { get; set; } = string.Empty;
}
