namespace Prm.Common.Models.Resources;

public class ResourceFilter
{
    public string? Status { get; set; }
    public string? Department { get; set; }
    public bool IncludeInactive { get; set; }
}
