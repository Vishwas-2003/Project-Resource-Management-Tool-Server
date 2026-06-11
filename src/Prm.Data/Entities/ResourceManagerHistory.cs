namespace Prm.Data.Entities;

public class ResourceManagerHistory : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ManagerUserId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public User User { get; set; } = null!;
    public User ManagerUser { get; set; } = null!;
}
