namespace Prm.Api.Models.Employees;

public class EmployeeListItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Status { get; set; }
}
