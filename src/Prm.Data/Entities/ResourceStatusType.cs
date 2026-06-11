namespace Prm.Data.Entities;

public class ResourceStatusType : BaseEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<ResourceStatusHistory> ResourceStatusHistories { get; set; } = new List<ResourceStatusHistory>();
}
