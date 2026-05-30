namespace Prm.Data.Entities;

public class Role : BaseEntity
{
    public int RoleId { get; set; }
    public required string Name { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
