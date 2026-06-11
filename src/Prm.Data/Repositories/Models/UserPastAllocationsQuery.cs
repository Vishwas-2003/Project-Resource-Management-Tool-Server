namespace Prm.Data.Repositories.Models;

public sealed class UserPastAllocationsQuery
{
    public int UserId { get; init; }
    public DateOnly AsOfDate { get; init; }
    public int Limit { get; init; }
}
