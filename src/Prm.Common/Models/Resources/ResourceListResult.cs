namespace Prm.Common.Models.Resources;

public class ResourceListResult
{
    public IReadOnlyList<ResourceSummary> Resources { get; set; } = [];
    public int Total { get; set; }
    public int Allocated { get; set; }
    public int Bench { get; set; }
}
