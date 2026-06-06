namespace Prm.Data.Repositories.Models;

public sealed class TeamTimesheetEntryRow
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int Hours { get; init; }
    public string Status { get; init; } = string.Empty;
}
