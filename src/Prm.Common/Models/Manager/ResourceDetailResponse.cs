namespace Prm.Common.Models.Manager;

public class ResourceDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public string ProfileSkills { get; set; } = string.Empty;
    public IReadOnlyList<ResourceAllocationRow> ActiveAllocations { get; set; } = [];
    public IReadOnlyList<ResourceAllocationRow> PastAllocations { get; set; } = [];
    public IReadOnlyList<string> RecentActivityTags { get; set; } = [];
}

public class ResourceAllocationRow
{
    public string Project { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
