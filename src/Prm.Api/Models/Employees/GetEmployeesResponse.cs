namespace Prm.Api.Models.Employees;

public class GetEmployeesResponse
{
    public IReadOnlyList<EmployeeListItemResponse> Employees { get; set; } = [];
    public int Total { get; set; }
    public int Allocated { get; set; }
    public int Bench { get; set; }
}
