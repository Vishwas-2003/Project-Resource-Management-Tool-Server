namespace Prm.Data.Entities;

public class Skill : BaseEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}
