namespace Prm.Data.Repositories.Models;

public class UserAllocationPeriodQuery
{
    public int UserId { get; init; }
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int? ExcludeAllocationId { get; init; }
}
