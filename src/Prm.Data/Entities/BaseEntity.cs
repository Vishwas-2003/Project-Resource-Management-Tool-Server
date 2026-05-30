namespace Prm.Data.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? ModifiedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public User? ModifiedByUser { get; set; }
}
