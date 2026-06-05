namespace Prm.Common.Models.Employees;

public class AssignManagerRequest
{
    public int EmployeeUserId { get; set; }
    public int ManagerUserId { get; set; }
    public required string Department { get; set; }
    public required string Designation { get; set; }
}
