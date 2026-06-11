namespace Prm.Data.Repositories.Models;

public sealed class ProjectAllocationOverlapQuery : UserAllocationPeriodQuery
{
    public int ProjectId { get; init; }
}
