namespace Prm.Common.Models.Employees;

public class EmployeeFilter
{
    public string? Status { get; set; }
    public string? Department { get; set; }
    public bool IncludeInactive { get; set; }
}
