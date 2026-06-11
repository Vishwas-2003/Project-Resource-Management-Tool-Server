namespace Prm.Data.Entities;

public class ResourceStatusHistory : BaseEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ResourceStatusTypeId { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public User User { get; set; } = null!;
    public ResourceStatusType ResourceStatusType { get; set; } = null!;
}
