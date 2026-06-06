namespace Prm.Data.Repositories.Models;

public class EmployeeAllocationPeriodQuery
{
    public int EmployeeId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int? ExcludeAllocationId { get; init; }
}
