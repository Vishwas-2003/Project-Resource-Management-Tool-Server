namespace Prm.Common.Models.Manager;

public class EmployeeUtilizationResponse
{
    public int EmployeeUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UtilizationPercent { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
}
