namespace Prm.Data.Repositories.Models;

public sealed class ProjectAllocationOverlapQuery : EmployeeAllocationPeriodQuery
{
    public int ProjectId { get; init; }
}
