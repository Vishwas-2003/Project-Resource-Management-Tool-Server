namespace Prm.Data.Entities;

public class Role : BaseEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
