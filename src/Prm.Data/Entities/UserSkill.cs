namespace Prm.Data.Entities;

public class UserSkill : BaseEntity
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public required string Proficiency { get; set; }
    public User User { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
