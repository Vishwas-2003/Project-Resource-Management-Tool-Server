namespace Prm.Data.Entities;

public class EmployeeSkill : BaseEntity
{
    public int EmployeeId { get; set; }
    public int SkillId { get; set; }
    public required string Proficiency { get; set; }
    public Employee Employee { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
