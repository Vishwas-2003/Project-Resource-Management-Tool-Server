namespace Prm.Common.Models.Allocations;

public class ActiveAllocationsResponse
{
    public IReadOnlyList<ActiveAllocationRow> Allocations { get; set; } = [];
    public int TotalActiveAllocations { get; set; }
}

public class ActiveAllocationRow
{
    public string EmployeeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}

