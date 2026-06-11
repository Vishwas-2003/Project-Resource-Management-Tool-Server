namespace Prm.Common.Models.Manager;

public class ResourceDashboardResponse
{
    public string PeriodLabel { get; set; } = string.Empty;
    public IReadOnlyList<BenchResourceRow> BenchResources { get; set; } = [];
    public IReadOnlyList<ActiveResourceRow> ActiveResources { get; set; } = [];
    public ResourceDashboardSummary Summary { get; set; } = new();
}

public class BenchResourceRow
{
    public int RowNumber { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
}

public class ActiveResourceRow
{
    public int RowNumber { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AllocationPercent { get; set; }
    public string Availability { get; set; } = string.Empty;
}

public class ResourceDashboardSummary
{
    public int BenchCount { get; set; }
    public int PartialCount { get; set; }
}
