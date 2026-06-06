namespace Prm.Data.Repositories.Models;

public sealed class EmployeePastAllocationsQuery
{
    public int EmployeeId { get; init; }
    public DateOnly AsOfDate { get; init; }
    public int Limit { get; init; }
}
