namespace Prm.Api.Models.Users;

public class GetUsersResponse
{
    public IReadOnlyList<UserListItemResponse> Users { get; set; } = [];
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
}
