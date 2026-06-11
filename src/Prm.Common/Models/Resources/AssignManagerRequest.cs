namespace Prm.Common.Models.Resources;

public class AssignManagerRequest
{
    public int ResourceUserId { get; set; }
    public int ManagerUserId { get; set; }
    public required string Department { get; set; }
    public required string Designation { get; set; }
}
