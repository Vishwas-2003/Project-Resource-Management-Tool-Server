namespace Prm.Common.Models.Timesheets;

public class ActivityTagItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ActivityTagOption
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOther { get; set; }
}

public class ActivityTagsResponse
{
    public IReadOnlyList<ActivityTagOption> Tags { get; set; } = [];
}

public class WeekAllocationItem
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public int ExpectedHours { get; set; }
}

public class WeekAllocationRow
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public int MaxHours { get; set; }
}

public class WeekAllocationsResponse
{
    public string ResourceName { get; set; } = string.Empty;
    public DateOnly WeekStart { get; set; }
    public int MaxWeeklyHours { get; set; }
    public IReadOnlyList<WeekAllocationRow> Allocations { get; set; } = [];
}

public class TimesheetEntryRequest
{
    public int ProjectId { get; set; }
    public int HoursWorked { get; set; }
    public IReadOnlyList<int> ActivityTagIds { get; set; } = [];
    public IReadOnlyList<string>? OtherActivityTags { get; set; }
    public IReadOnlyList<string>? ActivityTags { get; set; }
}

public class SubmitTimesheetRequest
{
    public DateOnly WeekStart { get; set; }
    public IReadOnlyList<TimesheetEntryRequest> Entries { get; set; } = [];
}

public class TimesheetWeekSummary
{
    public DateOnly WeekStart { get; set; }
    public int TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
}

public class SubmitTimesheetResponse
{
    public int TimesheetId { get; set; }
    public DateOnly WeekStart { get; set; }
    public int TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class MyTimesheetRow
{
    public int RowNumber { get; set; }
    public DateOnly WeekStart { get; set; }
    public int TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
}

public class MyTimesheetsResponse
{
    public IReadOnlyList<MyTimesheetRow> Timesheets { get; set; } = [];
}

public class TimesheetEntryDetail
{
    public string ProjectName { get; set; } = string.Empty;
    public int HoursWorked { get; set; }
    public IReadOnlyList<string> ActivityTags { get; set; } = [];
}

public class TimesheetWeekDetailResponse
{
    public DateOnly WeekStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalHours { get; set; }
    public string Access { get; set; } = string.Empty;
    public IReadOnlyList<TimesheetEntryDetail> Entries { get; set; } = [];
}

public class MissingTimesheetReminder
{
    public bool HasMissing { get; set; }
    public DateOnly? WeekStart { get; set; }
}

public class ResourceAllocationItem
{
    public string ProjectName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ResourceAllocationsResponse
{
    public IReadOnlyList<ResourceAllocationItem> Allocations { get; set; } = [];
    public int TotalUtilizationPercent { get; set; }
}

public class TeamTimesheetRow
{
    public int RowNumber { get; set; }
    public int ResourceUserId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int HoursWorked { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
}

public class TeamTimesheetsResponse
{
    public DateOnly WeekStart { get; set; }
    public IReadOnlyList<TeamTimesheetRow> Rows { get; set; } = [];
}

public class ResourceTimesheetDetailResponse
{
    public int ResourceUserId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public DateOnly WeekStart { get; set; }
    public int TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public IReadOnlyList<TimesheetEntryDetail> Entries { get; set; } = [];
}

public class RestoreTimesheetAccessResponse
{
    public int ResourceUserId { get; set; }
    public DateOnly WeekStart { get; set; }
    public string Access { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
