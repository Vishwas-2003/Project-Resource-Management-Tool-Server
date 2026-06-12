using System.ComponentModel.DataAnnotations;

namespace Prm.Api.Configuration;

public class HangfireOptions
{
    public const string Section = "Hangfire";
    public string DashboardPath { get; set; } = "/hangfire";
    public string RecurringJobId { get; set; } = "prm-scheduler";
    public string ProjectRiskAlertRecurringJobId { get; set; } = "prm-project-risk-alert";
    public int DefaultSchedulerIntervalMinutes { get; set; } = 60;
    public int ProjectRiskAlertHourUtc { get; set; } = 8;
    [Required]
    public string DashboardUsername { get; set; } = string.Empty;
    [Required]
    public string DashboardPassword { get; set; } = string.Empty;
}
