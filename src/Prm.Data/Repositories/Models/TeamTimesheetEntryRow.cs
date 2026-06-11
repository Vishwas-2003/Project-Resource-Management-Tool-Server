namespace Prm.Data.Repositories.Models;

public sealed class TeamTimesheetEntryRow
{
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int Hours { get; init; }
    public string Status { get; init; } = string.Empty;
}
