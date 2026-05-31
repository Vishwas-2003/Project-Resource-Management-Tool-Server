namespace Prm.Common.Models.Users;

public class UserListResult
{
    public IReadOnlyList<UserSummary> Users { get; set; } = [];
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
