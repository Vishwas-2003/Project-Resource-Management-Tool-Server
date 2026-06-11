namespace Prm.Common.Models.Resources;

public class ResourceSummary
{
    public int RowNumber { get; set; }
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Status { get; set; }
}
