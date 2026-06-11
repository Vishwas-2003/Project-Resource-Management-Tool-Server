namespace Prm.Common.Models.Manager;

public class ResourceUtilizationResponse
{
    public int ResourceUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
}
