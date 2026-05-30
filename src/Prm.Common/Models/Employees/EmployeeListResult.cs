namespace Prm.Common.Models.Employees;

public class EmployeeListResult
{
    public IReadOnlyList<EmployeeSummary> Employees { get; set; } = [];
    public int Total { get; set; }
    public int Allocated { get; set; }
    public int Bench { get; set; }
}
